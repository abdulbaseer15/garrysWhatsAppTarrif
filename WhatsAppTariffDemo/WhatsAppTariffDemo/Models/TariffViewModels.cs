namespace WhatsAppTariffDemo.Models
{
    public class TariffInputModel
    {
        public string AWBNumber { get; set; }
    }

    public class StorageResultModel
    {
        public string AWBNumber { get; set; }
        public string StationCode { get; set; }
        public string CargoType { get; set; }
        public decimal Weight { get; set; }
        public DateTime ArrivalDate { get; set; }
        public int TotalDays { get; set; }
        public int FreeDays { get; set; }
        public int ChargeableDays { get; set; }
        public decimal RateApplied { get; set; }
        public decimal MinimumCharge { get; set; }
        public decimal TaxPercentageApplied { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal EstimatedTotal { get; set; }
        public bool IsEscalated { get; set; }
        public string EscalationReason { get; set; }
    }

    public class ChatSessionState
    {
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public int CurrentStep { get; set; } = 1;
        public string TempAwb { get; set; }
        public string TempStation { get; set; }
        public string TempCargoType { get; set; }
        public DateTime TempDispatchDate { get; set; } // New: Dispatch Date
        public DateTime TempArrivalDate { get; set; }   // Arrival Date
        public decimal TempWeight { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsEscalated { get; set; }
        public int AttemptCount { get; set; }
    }

    public class ChatMessage
        {
            public string Sender { get; set; }
            public string Text { get; set; }
            public string Time { get; set; }
            public bool IsHtml { get; set; }
            public List<string> Options { get; set; } // Quick reply buttons
            public bool IsDatePicker { get; set; }     // Date picker flag
        }
    }