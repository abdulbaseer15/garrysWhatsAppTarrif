using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WhatsAppTariffDemo.Models;
using System.Collections.Generic;

namespace WhatsAppTariffDemo.Controllers
{
    public class AdminController : Controller
    {
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: Admin/Index - List all rules with DataTables support
        public IActionResult Index()
        {
            var rules = new List<TariffRuleModel>();
            string connString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(connString))
            {
                con.Open();
                // Updated query to include MinWeightSlab and MaxWeightSlab
                string query = "SELECT RuleId, StationCode, CargoType, FreeDays, RatePerKg, MinimumCharge, TaxPercentage, MinWeightSlab, MaxWeightSlab, LastUpdated FROM TariffRules ORDER BY StationCode, CargoType";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rules.Add(new TariffRuleModel
                            {
                                RuleId = Convert.ToInt32(reader["RuleId"]),
                                StationCode = reader["StationCode"].ToString(),
                                CargoType = reader["CargoType"].ToString(),
                                FreeDays = Convert.ToInt32(reader["FreeDays"]),
                                RatePerKg = Convert.ToDecimal(reader["RatePerKg"]),
                                MinimumCharge = Convert.ToDecimal(reader["MinimumCharge"]),
                                TaxPercentage = Convert.ToDecimal(reader["TaxPercentage"]),
                                MinWeightSlab = reader["MinWeightSlab"] != DBNull.Value ? Convert.ToDecimal(reader["MinWeightSlab"]) : 0,
                                MaxWeightSlab = reader["MaxWeightSlab"] != DBNull.Value ? Convert.ToDecimal(reader["MaxWeightSlab"]) : 0,
                                LastUpdated = reader["LastUpdated"] != DBNull.Value ? Convert.ToDateTime(reader["LastUpdated"]) : DateTime.Now
                            });
                        }
                    }
                }
            }
            return View(rules);
        }

        // GET: Admin/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Create
        [HttpPost]
        public IActionResult Create(TariffRuleModel model)
        {
            string connString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new SqlConnection(connString))
            {
                con.Open();
                string query = @"INSERT INTO TariffRules (StationCode, CargoType, FreeDays, RatePerKg, MinimumCharge, TaxPercentage, MinWeightSlab, MaxWeightSlab, EffectiveStartDate, EffectiveEndDate, HandlingCharges, LastUpdated) 
                                 VALUES (@Station, @Cargo, @FreeDays, @Rate, @MinCharge, @Tax, @MinWeight, @MaxWeight, @StartDate, @EndDate, @Handling, SYSDATETIME())";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Station", model.StationCode != null ? model.StationCode.ToUpper() : "");
                    cmd.Parameters.AddWithValue("@Cargo", model.CargoType ?? "");
                    cmd.Parameters.AddWithValue("@FreeDays", model.FreeDays);
                    cmd.Parameters.AddWithValue("@Rate", model.RatePerKg);
                    cmd.Parameters.AddWithValue("@MinCharge", model.MinimumCharge);
                    cmd.Parameters.AddWithValue("@Tax", model.TaxPercentage);
                    cmd.Parameters.AddWithValue("@MinWeight", model.MinWeightSlab);
                    cmd.Parameters.AddWithValue("@MaxWeight", model.MaxWeightSlab);
                    cmd.Parameters.AddWithValue("@StartDate", (object)model.EffectiveStartDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EndDate", (object)model.EffectiveEndDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Handling", model.HandlingCharges);

                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }

        // GET: Admin/Edit/5
        public IActionResult Edit(int id)
        {
            TariffRuleModel rule = null;
            string connString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(connString))
            {
                con.Open();
                // Updated query to fetch all fields for editing
                string query = "SELECT RuleId, StationCode, CargoType, FreeDays, RatePerKg, MinimumCharge, TaxPercentage, MinWeightSlab, MaxWeightSlab, EffectiveStartDate, EffectiveEndDate, HandlingCharges FROM TariffRules WHERE RuleId = @Id";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            rule = new TariffRuleModel
                            {
                                RuleId = Convert.ToInt32(reader["RuleId"]),
                                StationCode = reader["StationCode"].ToString(),
                                CargoType = reader["CargoType"].ToString(),
                                FreeDays = Convert.ToInt32(reader["FreeDays"]),
                                RatePerKg = Convert.ToDecimal(reader["RatePerKg"]),
                                MinimumCharge = Convert.ToDecimal(reader["MinimumCharge"]),
                                TaxPercentage = Convert.ToDecimal(reader["TaxPercentage"]),
                                MinWeightSlab = reader["MinWeightSlab"] != DBNull.Value ? Convert.ToDecimal(reader["MinWeightSlab"]) : 0,
                                MaxWeightSlab = reader["MaxWeightSlab"] != DBNull.Value ? Convert.ToDecimal(reader["MaxWeightSlab"]) : 0,
                                EffectiveStartDate = reader["EffectiveStartDate"] != DBNull.Value ? Convert.ToDateTime(reader["EffectiveStartDate"]) : null,
                                EffectiveEndDate = reader["EffectiveEndDate"] != DBNull.Value ? Convert.ToDateTime(reader["EffectiveEndDate"]) : null,
                                HandlingCharges = reader["HandlingCharges"] != DBNull.Value ? Convert.ToDecimal(reader["HandlingCharges"]) : 0
                            };
                        }
                    }
                }
            }
            if (rule == null) return NotFound();
            return View(rule);
        }

        // POST: Admin/Edit/5
        [HttpPost]
        public IActionResult Edit(TariffRuleModel model)
        {
            string connString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new SqlConnection(connString))
            {
                con.Open();
                string query = @"UPDATE TariffRules SET 
                             StationCode = @Station, 
                             CargoType = @Cargo, 
                             FreeDays = @FreeDays, 
                             RatePerKg = @Rate, 
                             MinimumCharge = @MinCharge, 
                             TaxPercentage = @Tax, 
                             MinWeightSlab = @MinWeight, 
                             MaxWeightSlab = @MaxWeight, 
                             EffectiveStartDate = @StartDate, 
                             EffectiveEndDate = @EndDate, 
                             HandlingCharges = @Handling, 
                             LastUpdated = SYSDATETIME() 
                             WHERE RuleId = @Id";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", model.RuleId);
                    cmd.Parameters.AddWithValue("@Station", model.StationCode != null ? model.StationCode.ToUpper() : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Cargo", model.CargoType ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FreeDays", model.FreeDays);
                    cmd.Parameters.AddWithValue("@Rate", model.RatePerKg);
                    cmd.Parameters.AddWithValue("@MinCharge", model.MinimumCharge);
                    cmd.Parameters.AddWithValue("@Tax", model.TaxPercentage);
                    cmd.Parameters.AddWithValue("@MinWeight", model.MinWeightSlab);
                    cmd.Parameters.AddWithValue("@MaxWeight", model.MaxWeightSlab);
                    cmd.Parameters.AddWithValue("@StartDate", (object)model.EffectiveStartDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EndDate", (object)model.EffectiveEndDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Handling", model.HandlingCharges);

                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }

        // GET: Admin/Delete/5
        public IActionResult Delete(int id)
        {
            string connString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new SqlConnection(connString))
            {
                con.Open();
                string query = "DELETE FROM TariffRules WHERE RuleId = @Id";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }
    }
}