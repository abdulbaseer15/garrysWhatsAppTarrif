using Microsoft.Data.SqlClient;

namespace WhatsAppTariffDemo.Services
{
    public class TariffResult
    {
        public bool Found { get; set; }
        public string Error { get; set; } = "";

        public int TotalDays { get; set; }
        public int FreeDays { get; set; }
        public int ChargeableDays { get; set; }

        public decimal Handling { get; set; }
        public decimal Rent { get; set; }
        public List<string> RentBreakdown { get; set; } = new();
        public List<(string Name, decimal Amount)> AwbFees { get; set; } = new();

        public decimal AwbFeesTotal => AwbFees.Sum(f => f.Amount);
        public decimal Subtotal => Handling + Rent + AwbFeesTotal;
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount => Math.Round(Subtotal * TaxPercentage / 100m, 0, MidpointRounding.AwayFromZero);
        public decimal Total => Subtotal + TaxAmount;
    }

    public static class TariffCalculator
    {
        // AFU tariff calculator.
        // Rent rule (matches dnata's official Tariff Calculator):
        //   1. totalDays = days from first date to second date, INCLUSIVE (same day = 1).
        //   2. Find the day slab that totalDays falls in (e.g. 11 - 999999).
        //   3. chargeableDays = totalDays - free days (free days = last day of a 0-rate slab).
        //   4. Rent = chargeableDays x rate of THAT slab (not tiered day by day).
        //      rate per day = flat rate if weight <= 50 kg, else per-kg rate x weight.
        public static TariffResult Calculate(string connString, string category, string cargoType,
            string station, decimal weight, DateTime dispatch, DateTime arrival)
        {
            var r = new TariffResult();

            r.TotalDays = (arrival.Date - dispatch.Date).Days + 1;
            if (r.TotalDays < 1) r.TotalDays = 1;

            using var con = new SqlConnection(connString);
            con.Open();

            // ---- Tax (station) ----
            using (var cmd = new SqlCommand("SELECT TaxPercentage FROM StationTax WHERE StationCode=@s", con))
            {
                cmd.Parameters.AddWithValue("@s", station);
                var t = cmd.ExecuteScalar();
                if (t == null) { r.Error = $"Station {station} not configured"; return r; }
                r.TaxPercentage = Convert.ToDecimal(t);
            }

            // ---- Handling (one time) ----
            const string handlingSql = @"
                SELECT FlatAmount, PerKgAmount FROM TariffHandling
                WHERE Category=@c AND CargoType=@t
                  AND @w > MinWeight AND @w <= MaxWeight
                  AND (EffectiveStartDate IS NULL OR @d >= EffectiveStartDate)
                  AND (EffectiveEndDate   IS NULL OR @d <= EffectiveEndDate)";
            using (var cmd = new SqlCommand(handlingSql, con))
            {
                cmd.Parameters.AddWithValue("@c", category);
                cmd.Parameters.AddWithValue("@t", cargoType);
                cmd.Parameters.AddWithValue("@w", weight);
                cmd.Parameters.AddWithValue("@d", arrival.Date);
                using var rd = cmd.ExecuteReader();
                if (!rd.Read()) { r.Error = "No handling slab for this weight/date"; return r; }
                r.Handling = Convert.ToDecimal(rd["FlatAmount"]) + Convert.ToDecimal(rd["PerKgAmount"]) * weight;
            }

            // ---- Rent ----
            const string rentSql = @"
                SELECT FromDay, ToDay, FlatPerDay, PerKgPerDay FROM TariffRent
                WHERE Category=@c AND CargoType=@t
                  AND (EffectiveStartDate IS NULL OR @d >= EffectiveStartDate)
                  AND (EffectiveEndDate   IS NULL OR @d <= EffectiveEndDate)
                ORDER BY FromDay";
            var slabs = new List<(int From, int To, decimal Flat, decimal PerKg)>();
            using (var cmd = new SqlCommand(rentSql, con))
            {
                cmd.Parameters.AddWithValue("@c", category);
                cmd.Parameters.AddWithValue("@t", cargoType);
                cmd.Parameters.AddWithValue("@d", arrival.Date);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                    slabs.Add((Convert.ToInt32(rd["FromDay"]), Convert.ToInt32(rd["ToDay"]),
                               Convert.ToDecimal(rd["FlatPerDay"]), Convert.ToDecimal(rd["PerKgPerDay"])));
            }
            if (slabs.Count == 0) { r.Error = "No rent slabs configured"; return r; }

            // free days = end of a slab whose rate is 0 (General: day 3). Others: 0.
            var freeSlab = slabs.FirstOrDefault(s => s.Flat == 0 && s.PerKg == 0);
            r.FreeDays = freeSlab.To > 0 ? Math.Min(r.TotalDays, freeSlab.To) : 0;

            // slab that the total stay falls in
            var slab = slabs.FirstOrDefault(s => r.TotalDays >= s.From && r.TotalDays <= s.To);
            if (slab.To == 0) { r.Error = "No rent slab covers this number of days"; return r; }

            r.ChargeableDays = Math.Max(0, r.TotalDays - r.FreeDays);

            decimal perDay = weight <= 50 ? slab.Flat : slab.PerKg * weight;
            if (perDay > 0 && r.ChargeableDays > 0)
            {
                r.Rent = perDay * r.ChargeableDays;
                string toLabel = slab.To >= 999999 ? "and above" : "to " + slab.To;
                r.RentBreakdown.Add(
                    $"{r.ChargeableDays} chargeable day(s) x PKR {perDay:N2} = PKR {r.Rent:N2} (day slab {slab.From} {toLabel})");
            }

            // ---- AWB fees ----
            using (var cmd = new SqlCommand("SELECT FeeName, Amount FROM TariffAwbFees WHERE Category=@c AND IsActive=1", con))
            {
                cmd.Parameters.AddWithValue("@c", category);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                    r.AwbFees.Add((rd["FeeName"].ToString()!, Convert.ToDecimal(rd["Amount"])));
            }

            r.Found = true;
            return r;
        }
    }
}