namespace WhatsAppTariffDemo.Models
{
    public class TariffRuleModel
    {
        public int RuleId { get; set; }
        public string StationCode { get; set; }
        public string CargoType { get; set; }
        public int FreeDays { get; set; }
        public decimal RatePerKg { get; set; }
        public decimal MinimumCharge { get; set; }
        public decimal TaxPercentage { get; set; }
        public DateTime LastUpdated { get; set; }



        public DateTime? EffectiveStartDate { get; set; }
        public DateTime? EffectiveEndDate { get; set; }
        public decimal MinWeightSlab { get; set; }
        public decimal MaxWeightSlab { get; set; }
     
        public decimal HandlingCharges { get; set; }
    }
}