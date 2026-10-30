//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Data.SqlClient;
//using System.Text.Json;
//using WhatsAppTariffDemo.Models;
//using WhatsAppTariffDemo.Services;

//namespace WhatsAppTariffDemo.Controllers
//{
//    public class TariffController : Controller
//    {
//        private readonly IConfiguration _configuration;

//        public TariffController(IConfiguration configuration)
//        {
//            _configuration = configuration;
//        }

//        [HttpGet]
//        public IActionResult Index()
//        {
//            var state = new ChatSessionState();
//            state.CurrentStep = 1; // Step 1: Ask AWB
//            state.Messages.Add(new ChatMessage
//            {
//                Sender = "Bot",
//                Text = "👋 Welcome to Gerry's dnata Import Cargo Storage Estimation Bot.\n\n📦 Please enter your **AWB / HAWB Number**:",
//                Time = DateTime.Now.ToString("hh:mm tt")
//            });

//            return View(state);
//        }

//        private string GetWhatsAppEscalationHtml(ChatSessionState state, string reason)
//        {
//            string encodedText = System.Net.WebUtility.UrlEncode(
//                $"⚠️ Cargo Tariff Bot Escalation!\n" +
//                $"• Issue: {reason}\n" +
//                $"• AWB: {state.TempAwb ?? "N/A"}\n" +
//                $"• Station: {state.TempStation ?? "N/A"}\n" +
//                $"• Cargo Type: {state.TempCargoType ?? "N/A"}\n" +
//                $"Please assist me."
//            );
//            string waLink = $"https://wa.me/923039200623?text={encodedText}";

//            return $"⚠️ {reason} (3/3). Handed over to **Cargo Ops Desk**.<br><br>" +
//                   $"<a href='{waLink}' target='_blank' style='display: inline-block; background-color: #00a884; color: white; padding: 8px 14px; border-radius: 6px; text-decoration: none; font-weight: 600; margin-top: 5px;'>" +
//                   $"<i class='fa-brands fa-whatsapp'></i> Chat with Cargo Ops on WhatsApp" +
//                   $"</a>";
//        }

//        [HttpPost]
//        public IActionResult Index(string userInput, string stateJson)
//        {
//            ChatSessionState state;

//            try
//            {
//                state = JsonSerializer.Deserialize<ChatSessionState>(stateJson) ?? new ChatSessionState();
//            }
//            catch
//            {
//                state = new ChatSessionState();
//            }

//            if (string.IsNullOrWhiteSpace(userInput))
//            {
//                return View(state);
//            }

//            state.Messages.Add(new ChatMessage
//            {
//                Sender = "User",
//                Text = userInput,
//                Time = DateTime.Now.ToString("hh:mm tt")
//            });

//            if (state.IsCompleted || state.IsEscalated)
//            {
//                return View(state);
//            }

//            string connString = _configuration.GetConnectionString("DefaultConnection");

//            try
//            {
//                switch (state.CurrentStep)
//                {
//                    case 1: // Received AWB, Ask Station via Options
//                        state.TempAwb = userInput.Trim();
//                        state.CurrentStep = 2;
//                        state.Messages.Add(new ChatMessage
//                        {
//                            Sender = "Bot",
//                            Text = $"Got it (AWB: **{state.TempAwb}**).\n\n🏢 Please select your **Station Code**:",
//                            Time = DateTime.Now.ToString("hh:mm tt"),
//                            IsHtml = true,
//                            Options = new List<string> { "KHI", "LHE", "ISB", "MUX" }
//                        });
//                        break;

//                    case 2: // Received Station, Ask Cargo Type via Options
//                        state.TempStation = userInput.Trim().ToUpper();
//                        state.CurrentStep = 3;
//                        state.Messages.Add(new ChatMessage
//                        {
//                            Sender = "Bot",
//                            Text = $"Station: **{state.TempStation}**.\n\n📦 Please select your **Cargo Category / Type**:",
//                            Time = DateTime.Now.ToString("hh:mm tt"),
//                            IsHtml = true,
//                            // Spelling must match CargoType in TariffHandling / TariffRent exactly
//                            Options = new List<string>
//                            {
//                                "General",
//                                "Special (Dangerous Goods)",
//                                "Cold Storage (2-8°C)",
//                                "Cold Storage (15-25°C)",
//                                "Cold Storage (Freezer)"
//                            }
//                        });
//                        break;

//                    case 3: // Received Cargo Type, Ask Dispatch Date
//                        state.TempCargoType = userInput.Trim();
//                        state.CurrentStep = 4;
//                        state.Messages.Add(new ChatMessage
//                        {
//                            Sender = "Bot",
//                            Text = $"Cargo: **{state.TempCargoType}**.\n\n📅 Please select the **Arrival Date** (date the cargo arrived):",
//                            Time = DateTime.Now.ToString("hh:mm tt"),
//                            IsHtml = true,
//                            IsDatePicker = true
//                        });
//                        break;

//                    case 4: // Received Dispatch Date, Ask Arrival Date
//                        if (!DateTime.TryParse(userInput.Trim(), out DateTime parsedDispatchDate) || parsedDispatchDate.Year < 1753 || parsedDispatchDate > DateTime.Now)
//                        {
//                            state.AttemptCount++;
//                            if (state.AttemptCount >= 3)
//                            {
//                                state.IsEscalated = true;
//                                state.Messages.Add(new ChatMessage
//                                {
//                                    Sender = "Bot",
//                                    Text = GetWhatsAppEscalationHtml(state, "Maximum invalid arrival date attempts reached"),
//                                    Time = DateTime.Now.ToString("hh:mm tt"),
//                                    IsHtml = true
//                                });
//                                break;
//                            }

//                            state.Messages.Add(new ChatMessage
//                            {
//                                Sender = "Bot",
//                                Text = $"⚠️ Invalid Arrival Date! Please select a valid past date. (Attempts: {state.AttemptCount}/3)",
//                                Time = DateTime.Now.ToString("hh:mm tt"),
//                                IsHtml = true
//                            });
//                            break;
//                        }

//                        state.TempDispatchDate = parsedDispatchDate;
//                        state.CurrentStep = 5;
//                        state.AttemptCount = 0; // Reset
//                        state.Messages.Add(new ChatMessage
//                        {
//                            Sender = "Bot",
//                            Text = $"Arrival Date: **{state.TempDispatchDate:yyyy-MM-dd}**.\n\n📅 Now please select the **Payment Date**:",
//                            Time = DateTime.Now.ToString("hh:mm tt"),
//                            IsHtml = true,
//                            IsDatePicker = true
//                        });
//                        break;

//                    case 5: // Received Arrival Date, Ask Weight
//                        if (!DateTime.TryParse(userInput.Trim(), out DateTime parsedArrivalDate) || parsedArrivalDate.Year < 1753 || parsedArrivalDate < state.TempDispatchDate)
//                        {
//                            state.AttemptCount++;
//                            if (state.AttemptCount >= 3)
//                            {
//                                state.IsEscalated = true;
//                                state.Messages.Add(new ChatMessage
//                                {
//                                    Sender = "Bot",
//                                    Text = GetWhatsAppEscalationHtml(state, "Maximum invalid payment date attempts reached"),
//                                    Time = DateTime.Now.ToString("hh:mm tt"),
//                                    IsHtml = true
//                                });
//                                break;
//                            }

//                            state.Messages.Add(new ChatMessage
//                            {
//                                Sender = "Bot",
//                                Text = $"⚠️ Invalid Payment Date! Payment date must be on or after the arrival date ({state.TempDispatchDate:yyyy-MM-dd}). (Attempts: {state.AttemptCount}/3)",
//                                Time = DateTime.Now.ToString("hh:mm tt"),
//                                IsHtml = true
//                            });
//                            break;
//                        }

//                        state.TempArrivalDate = parsedArrivalDate;
//                        state.CurrentStep = 6;
//                        state.AttemptCount = 0; // Reset
//                        state.Messages.Add(new ChatMessage
//                        {
//                            Sender = "Bot",
//                            Text = "⚖️ Please enter the total **Weight in Kilograms** (e.g., `150`):",
//                            Time = DateTime.Now.ToString("hh:mm tt")
//                        });
//                        break;

//                    case 6: // Weight received -> calculate using new tariff tables
//                        if (!decimal.TryParse(userInput.Trim(), out decimal parsedWeight) || parsedWeight <= 0)
//                        {
//                            state.AttemptCount++;
//                            if (state.AttemptCount >= 3)
//                            {
//                                state.IsEscalated = true;
//                                state.Messages.Add(new ChatMessage
//                                {
//                                    Sender = "Bot",
//                                    Text = GetWhatsAppEscalationHtml(state, "Maximum invalid weight attempts reached"),
//                                    Time = DateTime.Now.ToString("hh:mm tt"),
//                                    IsHtml = true
//                                });
//                                break;
//                            }

//                            state.Messages.Add(new ChatMessage
//                            {
//                                Sender = "Bot",
//                                Text = $"⚠️ Invalid weight! Please enter a valid numeric weight in kilograms. (Attempts: {state.AttemptCount}/3)",
//                                Time = DateTime.Now.ToString("hh:mm tt")
//                            });
//                            break;
//                        }
//                        state.TempWeight = parsedWeight;

//                        // All currently loaded cargo types belong to category AFU
//                        var calc = TariffCalculator.Calculate(connString, "AFU", state.TempCargoType,
//                            state.TempStation, state.TempWeight, state.TempDispatchDate, state.TempArrivalDate);

//                        if (!calc.Found)
//                        {
//                            state.AttemptCount++;
//                            if (state.AttemptCount >= 3)
//                            {
//                                state.IsEscalated = true;
//                                state.Messages.Add(new ChatMessage
//                                {
//                                    Sender = "Bot",
//                                    Text = GetWhatsAppEscalationHtml(state, "Tariff rule not found: " + calc.Error),
//                                    Time = DateTime.Now.ToString("hh:mm tt"),
//                                    IsHtml = true
//                                });
//                                break;
//                            }

//                            state.Messages.Add(new ChatMessage
//                            {
//                                Sender = "Bot",
//                                Text = $"⚠️ **Tariff Rule Not Found:** {calc.Error}. (Attempt {state.AttemptCount}/3)",
//                                Time = DateTime.Now.ToString("hh:mm tt"),
//                                IsHtml = true
//                            });
//                            break;
//                        }

//                        state.AttemptCount = 0;

//                        // Save calculation log to StorageCalculations table
//                        using (SqlConnection con = new SqlConnection(connString))
//                        {
//                            con.Open();
//                            string insertCalc = @"INSERT INTO StorageCalculations
//                                (AWBNumber, StationCode, CargoType, DispatchDate, ArrivalDate, Weight, TotalDays, FreeDays, ChargeableDays, RateApplied, Subtotal, TaxAmount, EstimatedTotal)
//                                VALUES (@AWB, @Station, @CargoType, @DispatchDate, @ArrivalDate, @Weight, @TotalDays, @FreeDays, @ChargeableDays, @Rate, @Subtotal, @TaxAmt, @Total)";

//                            using (SqlCommand logCmd = new SqlCommand(insertCalc, con))
//                            {
//                                logCmd.Parameters.AddWithValue("@AWB", state.TempAwb);
//                                logCmd.Parameters.AddWithValue("@Station", state.TempStation);
//                                logCmd.Parameters.AddWithValue("@CargoType", state.TempCargoType);
//                                logCmd.Parameters.AddWithValue("@DispatchDate", state.TempDispatchDate);
//                                logCmd.Parameters.AddWithValue("@ArrivalDate", state.TempArrivalDate);
//                                logCmd.Parameters.AddWithValue("@Weight", state.TempWeight);
//                                logCmd.Parameters.AddWithValue("@TotalDays", calc.TotalDays);
//                                logCmd.Parameters.AddWithValue("@FreeDays", calc.FreeDays);
//                                logCmd.Parameters.AddWithValue("@ChargeableDays", calc.ChargeableDays);
//                                // average rent per chargeable day (rate varies by day slab)
//                                logCmd.Parameters.AddWithValue("@Rate", calc.ChargeableDays > 0 ? Math.Round(calc.Rent / calc.ChargeableDays, 2) : 0m);
//                                logCmd.Parameters.AddWithValue("@Subtotal", calc.Subtotal);
//                                logCmd.Parameters.AddWithValue("@TaxAmt", calc.TaxAmount);
//                                logCmd.Parameters.AddWithValue("@Total", calc.Total);
//                                logCmd.ExecuteNonQuery();
//                            }
//                        }

//                        string rentLines = calc.RentBreakdown.Count > 0
//                            ? string.Join("\n", calc.RentBreakdown.Select(x => "   • " + x)) + "\n"
//                            : "   • No rent (within free days)\n";
//                        string feeLines = string.Join("\n", calc.AwbFees.Select(f => $"   • {f.Name}: PKR {f.Amount:N2}"));

//                        string botResponse =
//                            $"🤖 **Tariff Calculation Successful:**\n\n" +
//                            $"📦 **AWB:** {state.TempAwb}\n" +
//                            $"🏢 **Station:** {state.TempStation} | **Cargo:** {state.TempCargoType}\n" +
//                            $"📤 **Arrival Date:** {state.TempDispatchDate:yyyy-MM-dd}\n" +
//                            $"📅 **Payment Date:** {state.TempArrivalDate:yyyy-MM-dd}\n" +
//                            $"⚖️ **Weight:** {state.TempWeight} kg\n" +
//                            $"⏳ **Total Days:** {calc.TotalDays} (Free: {calc.FreeDays}, Chargeable: {calc.ChargeableDays})\n\n" +
//                            $"📦 **Handling Charges:** PKR {calc.Handling:N2}\n" +
//                            $"🏚️ **Godown Rent:** PKR {calc.Rent:N2}\n{rentLines}\n" +
//                            $"🧾 **Per Airway Bill Charges:**\n{feeLines}\n\n" +
//                            $"📈 **Subtotal:** PKR {calc.Subtotal:N2}\n" +
//                            $"🏛️ **Tax ({calc.TaxPercentage}%):** PKR {calc.TaxAmount:N2}\n\n" +
//                            $"💰 **TOTAL ESTIMATED: PKR {calc.Total:N2}**\n\n" +
//                            $"*(Note: Estimate only, subject to terminal verification.)*";

//                        state.Messages.Add(new ChatMessage
//                        {
//                            Sender = "Bot",
//                            Text = botResponse,
//                            Time = DateTime.Now.ToString("hh:mm tt"),
//                            IsHtml = true
//                        });

//                        state.IsCompleted = true;
//                        break;
//                }
//            }
//            catch (Exception ex)
//            {
//                state.Messages.Add(new ChatMessage
//                {
//                    Sender = "Bot",
//                    Text = $"⚠️ Error: {ex.Message}",
//                    Time = DateTime.Now.ToString("hh:mm tt")
//                });
//            }

//            return View(state);
//        }
//    }
//}



using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Text.Json;
using WhatsAppTariffDemo.Models;
using WhatsAppTariffDemo.Services;

namespace WhatsAppTariffDemo.Controllers
{
    public class TariffController : Controller
    {
        private readonly IConfiguration _configuration;

        public TariffController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Dates are shown as "27 Sep 2026" so day/month can never be confused
        private static string D(DateTime d) => d.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

        [HttpGet]
        public IActionResult Index()
        {
            var state = new ChatSessionState();
            state.CurrentStep = 1; // Step 1: Ask AWB
            state.Messages.Add(new ChatMessage
            {
                Sender = "Bot",
                Text = "👋 Welcome to Gerry's dnata Import Cargo Storage Estimation Bot.\n\n📦 Please enter your **AWB / HAWB Number**:",
                Time = DateTime.Now.ToString("hh:mm tt")
            });

            return View(state);
        }

        private string GetWhatsAppEscalationHtml(ChatSessionState state, string reason)
        {
            string encodedText = System.Net.WebUtility.UrlEncode(
                $"⚠️ Cargo Tariff Bot Escalation!\n" +
                $"• Issue: {reason}\n" +
                $"• AWB: {state.TempAwb ?? "N/A"}\n" +
                $"• Station: {state.TempStation ?? "N/A"}\n" +
                $"• Cargo Type: {state.TempCargoType ?? "N/A"}\n" +
                $"Please assist me."
            );
            string waLink = $"https://wa.me/923039200623?text={encodedText}";

            return $"⚠️ {reason} (3/3). Handed over to **Cargo Ops Desk**.<br><br>" +
                   $"<a href='{waLink}' target='_blank' style='display: inline-block; background-color: #00a884; color: white; padding: 8px 14px; border-radius: 6px; text-decoration: none; font-weight: 600; margin-top: 5px;'>" +
                   $"<i class='fa-brands fa-whatsapp'></i> Chat with Cargo Ops on WhatsApp" +
                   $"</a>";
        }

        [HttpPost]
        public IActionResult Index(string userInput, string stateJson)
        {
            ChatSessionState state;

            try
            {
                state = JsonSerializer.Deserialize<ChatSessionState>(stateJson) ?? new ChatSessionState();
            }
            catch
            {
                state = new ChatSessionState();
            }

            if (string.IsNullOrWhiteSpace(userInput))
            {
                return View(state);
            }

            // On the date steps the picker sends yyyy-MM-dd; show it to the user as "27 Sep 2026"
            string shownInput = userInput;
            if ((state.CurrentStep == 4 || state.CurrentStep == 5) &&
                DateTime.TryParseExact(userInput.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime shownDate))
            {
                shownInput = D(shownDate);
            }

            state.Messages.Add(new ChatMessage
            {
                Sender = "User",
                Text = shownInput,
                Time = DateTime.Now.ToString("hh:mm tt")
            });

            if (state.IsCompleted || state.IsEscalated)
            {
                return View(state);
            }

            string connString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                switch (state.CurrentStep)
                {
                    case 1: // Received AWB, Ask Station via Options
                        state.TempAwb = userInput.Trim();
                        state.CurrentStep = 2;
                        state.Messages.Add(new ChatMessage
                        {
                            Sender = "Bot",
                            Text = $"Got it (AWB: **{state.TempAwb}**).\n\n🏢 Please select your **Station Code**:",
                            Time = DateTime.Now.ToString("hh:mm tt"),
                            IsHtml = true,
                            Options = new List<string> { "KHI", "LHE", "ISB", "MUX" }
                        });
                        break;

                    case 2: // Received Station, Ask Cargo Type via Options
                        state.TempStation = userInput.Trim().ToUpper();
                        state.CurrentStep = 3;
                        state.Messages.Add(new ChatMessage
                        {
                            Sender = "Bot",
                            Text = $"Station: **{state.TempStation}**.\n\n📦 Please select your **Cargo Category / Type**:",
                            Time = DateTime.Now.ToString("hh:mm tt"),
                            IsHtml = true,
                            // Spelling must match CargoType in TariffHandling / TariffRent exactly
                            Options = new List<string>
                            {
                                "General",
                                "Special (Dangerous Goods)",
                                "Cold Storage (2-8°C)",
                                "Cold Storage (15-25°C)",
                                "Cold Storage (Freezer)"
                            }
                        });
                        break;

                    case 3: // Received Cargo Type, Ask Dispatch Date
                        state.TempCargoType = userInput.Trim();
                        state.CurrentStep = 4;
                        state.Messages.Add(new ChatMessage
                        {
                            Sender = "Bot",
                            Text = $"Cargo: **{state.TempCargoType}**.\n\n📅 Please select the **Arrival Date** (date the cargo arrived):",
                            Time = DateTime.Now.ToString("hh:mm tt"),
                            IsHtml = true,
                            IsDatePicker = true
                        });
                        break;

                    case 4: // Received Dispatch Date, Ask Arrival Date
                        if (!DateTime.TryParse(userInput.Trim(), out DateTime parsedDispatchDate) || parsedDispatchDate.Year < 1753 || parsedDispatchDate > DateTime.Now)
                        {
                            state.AttemptCount++;
                            if (state.AttemptCount >= 3)
                            {
                                state.IsEscalated = true;
                                state.Messages.Add(new ChatMessage
                                {
                                    Sender = "Bot",
                                    Text = GetWhatsAppEscalationHtml(state, "Maximum invalid arrival date attempts reached"),
                                    Time = DateTime.Now.ToString("hh:mm tt"),
                                    IsHtml = true
                                });
                                break;
                            }

                            state.Messages.Add(new ChatMessage
                            {
                                Sender = "Bot",
                                Text = $"⚠️ Invalid Arrival Date! Please select a valid past date. (Attempts: {state.AttemptCount}/3)",
                                Time = DateTime.Now.ToString("hh:mm tt"),
                                IsHtml = true
                            });
                            break;
                        }

                        state.TempDispatchDate = parsedDispatchDate;
                        state.CurrentStep = 5;
                        state.AttemptCount = 0; // Reset
                        state.Messages.Add(new ChatMessage
                        {
                            Sender = "Bot",
                            Text = $"Arrival Date: **{D(state.TempDispatchDate)}**.\n\n📅 Now please select the **Payment Date** (usually today):",
                            Time = DateTime.Now.ToString("hh:mm tt"),
                            IsHtml = true,
                            IsDatePicker = true
                        });
                        break;

                    case 5: // Received Arrival Date, Ask Weight
                        if (!DateTime.TryParse(userInput.Trim(), out DateTime parsedArrivalDate) || parsedArrivalDate.Year < 1753 || parsedArrivalDate < state.TempDispatchDate)
                        {
                            state.AttemptCount++;
                            if (state.AttemptCount >= 3)
                            {
                                state.IsEscalated = true;
                                state.Messages.Add(new ChatMessage
                                {
                                    Sender = "Bot",
                                    Text = GetWhatsAppEscalationHtml(state, "Maximum invalid payment date attempts reached"),
                                    Time = DateTime.Now.ToString("hh:mm tt"),
                                    IsHtml = true
                                });
                                break;
                            }

                            state.Messages.Add(new ChatMessage
                            {
                                Sender = "Bot",
                                Text = $"⚠️ Invalid Payment Date! Payment date must be on or after the arrival date ({D(state.TempDispatchDate)}). (Attempts: {state.AttemptCount}/3)",
                                Time = DateTime.Now.ToString("hh:mm tt"),
                                IsHtml = true
                            });
                            break;
                        }

                        state.TempArrivalDate = parsedArrivalDate;
                        state.CurrentStep = 6;
                        state.AttemptCount = 0; // Reset
                        state.Messages.Add(new ChatMessage
                        {
                            Sender = "Bot",
                            Text = "⚖️ Please enter the total **Weight in Kilograms** (e.g., `150`):",
                            Time = DateTime.Now.ToString("hh:mm tt")
                        });
                        break;

                    case 6: // Weight received -> calculate using new tariff tables
                        if (!decimal.TryParse(userInput.Trim(), out decimal parsedWeight) || parsedWeight <= 0)
                        {
                            state.AttemptCount++;
                            if (state.AttemptCount >= 3)
                            {
                                state.IsEscalated = true;
                                state.Messages.Add(new ChatMessage
                                {
                                    Sender = "Bot",
                                    Text = GetWhatsAppEscalationHtml(state, "Maximum invalid weight attempts reached"),
                                    Time = DateTime.Now.ToString("hh:mm tt"),
                                    IsHtml = true
                                });
                                break;
                            }

                            state.Messages.Add(new ChatMessage
                            {
                                Sender = "Bot",
                                Text = $"⚠️ Invalid weight! Please enter a valid numeric weight in kilograms. (Attempts: {state.AttemptCount}/3)",
                                Time = DateTime.Now.ToString("hh:mm tt")
                            });
                            break;
                        }
                        state.TempWeight = parsedWeight;

                        // All currently loaded cargo types belong to category AFU
                        var calc = TariffCalculator.Calculate(connString, "AFU", state.TempCargoType,
                            state.TempStation, state.TempWeight, state.TempDispatchDate, state.TempArrivalDate);

                        if (!calc.Found)
                        {
                            state.AttemptCount++;
                            if (state.AttemptCount >= 3)
                            {
                                state.IsEscalated = true;
                                state.Messages.Add(new ChatMessage
                                {
                                    Sender = "Bot",
                                    Text = GetWhatsAppEscalationHtml(state, "Tariff rule not found: " + calc.Error),
                                    Time = DateTime.Now.ToString("hh:mm tt"),
                                    IsHtml = true
                                });
                                break;
                            }

                            state.Messages.Add(new ChatMessage
                            {
                                Sender = "Bot",
                                Text = $"⚠️ **Tariff Rule Not Found:** {calc.Error}. (Attempt {state.AttemptCount}/3)",
                                Time = DateTime.Now.ToString("hh:mm tt"),
                                IsHtml = true
                            });
                            break;
                        }

                        state.AttemptCount = 0;

                        // Save calculation log to StorageCalculations table
                        using (SqlConnection con = new SqlConnection(connString))
                        {
                            con.Open();
                            string insertCalc = @"INSERT INTO StorageCalculations
                                (AWBNumber, StationCode, CargoType, DispatchDate, ArrivalDate, Weight, TotalDays, FreeDays, ChargeableDays, RateApplied, Subtotal, TaxAmount, EstimatedTotal)
                                VALUES (@AWB, @Station, @CargoType, @DispatchDate, @ArrivalDate, @Weight, @TotalDays, @FreeDays, @ChargeableDays, @Rate, @Subtotal, @TaxAmt, @Total)";

                            using (SqlCommand logCmd = new SqlCommand(insertCalc, con))
                            {
                                logCmd.Parameters.AddWithValue("@AWB", state.TempAwb);
                                logCmd.Parameters.AddWithValue("@Station", state.TempStation);
                                logCmd.Parameters.AddWithValue("@CargoType", state.TempCargoType);
                                logCmd.Parameters.AddWithValue("@DispatchDate", state.TempDispatchDate);
                                logCmd.Parameters.AddWithValue("@ArrivalDate", state.TempArrivalDate);
                                logCmd.Parameters.AddWithValue("@Weight", state.TempWeight);
                                logCmd.Parameters.AddWithValue("@TotalDays", calc.TotalDays);
                                logCmd.Parameters.AddWithValue("@FreeDays", calc.FreeDays);
                                logCmd.Parameters.AddWithValue("@ChargeableDays", calc.ChargeableDays);
                                // average rent per chargeable day (rate varies by day slab)
                                logCmd.Parameters.AddWithValue("@Rate", calc.ChargeableDays > 0 ? Math.Round(calc.Rent / calc.ChargeableDays, 2) : 0m);
                                logCmd.Parameters.AddWithValue("@Subtotal", calc.Subtotal);
                                logCmd.Parameters.AddWithValue("@TaxAmt", calc.TaxAmount);
                                logCmd.Parameters.AddWithValue("@Total", calc.Total);
                                logCmd.ExecuteNonQuery();
                            }
                        }

                        string rentLines = calc.RentBreakdown.Count > 0
                            ? string.Join("\n", calc.RentBreakdown.Select(x => "   • " + x)) + "\n"
                            : "   • No rent (within free days)\n";
                        string feeLines = string.Join("\n", calc.AwbFees.Select(f => $"   • {f.Name}: PKR {f.Amount:N2}"));

                        string botResponse =
                            $"🤖 **Tariff Calculation Successful:**\n\n" +
                            $"📦 **AWB:** {state.TempAwb}\n" +
                            $"🏢 **Station:** {state.TempStation} | **Cargo:** {state.TempCargoType}\n" +
                            $"📤 **Arrival Date:** {D(state.TempDispatchDate)}\n" +
                            $"📅 **Payment Date:** {D(state.TempArrivalDate)}\n" +
                            $"⚖️ **Weight:** {state.TempWeight} kg\n" +
                            $"⏳ **Total Days:** {calc.TotalDays} (Free: {calc.FreeDays}, Chargeable: {calc.ChargeableDays})\n\n" +
                            $"📦 **Handling Charges:** PKR {calc.Handling:N2}\n" +
                            $"🏚️ **Godown Rent:** PKR {calc.Rent:N2}\n{rentLines}\n" +
                            $"🧾 **Per Airway Bill Charges:**\n{feeLines}\n\n" +
                            $"📈 **Subtotal:** PKR {calc.Subtotal:N2}\n" +
                            $"🏛️ **Tax ({calc.TaxPercentage}%):** PKR {calc.TaxAmount:N2}\n\n" +
                            $"💰 **TOTAL ESTIMATED: PKR {calc.Total:N2}**\n\n" +
                            $"*(Note: Estimate only, subject to terminal verification.)*";

                        state.Messages.Add(new ChatMessage
                        {
                            Sender = "Bot",
                            Text = botResponse,
                            Time = DateTime.Now.ToString("hh:mm tt"),
                            IsHtml = true
                        });

                        state.IsCompleted = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                state.Messages.Add(new ChatMessage
                {
                    Sender = "Bot",
                    Text = $"⚠️ Error: {ex.Message}",
                    Time = DateTime.Now.ToString("hh:mm tt")
                });
            }

            return View(state);
        }
    }
}