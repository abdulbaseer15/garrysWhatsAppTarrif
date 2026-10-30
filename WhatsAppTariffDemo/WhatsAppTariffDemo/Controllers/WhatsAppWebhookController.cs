using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace WhatsAppTariffDemo.Controllers
{
    [Route("api/whatsapp")]
    [ApiController]
    public class WhatsAppWebhookController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public WhatsAppWebhookController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public IActionResult ReceiveMessage([FromForm] string Body, [FromForm] string From)
        {
            string incomingMsg = Body?.Trim() ?? "";
            string replyMessage = "🤖 Welcome to Gerry's dnata Cargo Bot!\n\nAapka message mil gaya hai. Mazeed steps ke liye apna AWB number bhejein:";

            string connString = _configuration.GetConnectionString("DefaultConnection");

            // Yahan aap apne chat session ya simple keyword logic laga sakte hain
            // Misal ke taur par agar user ne AWB ya weight bheja hai, toh database se query kar ke rate nikal len.

            if (incomingMsg.StartsWith("AWB", StringComparison.OrdinalIgnoreCase) || incomingMsg.Length > 5)
            {
                replyMessage = $"📦 AWB: {incomingMsg}\n\n🏢 Ab apna Station Code bhejein (jaise: KHI, LHE, ISB, MUX):";
            }
            else if (incomingMsg.ToUpper() == "KHI" || incomingMsg.ToUpper() == "LHE" || incomingMsg.ToUpper() == "ISB" || incomingMsg.ToUpper() == "MUX")
            {
                replyMessage = $"Station: {incomingMsg.ToUpper()} selected.\n\n📦 Ab Cargo Type bhejein (`General` ya `Special`):";
            }
            // Aap apne purane ChatSessionState logic ko bhi yahan connect kar sakte hain!

            // Twilio XML Response format (TwiML)
            string twiMlResponse = $@"<Response>
    <Message>{System.Net.WebUtility.HtmlEncode(replyMessage)}</Message>
</Response>";

            return Content(twiMlResponse, "application/xml");
        }
    }
}