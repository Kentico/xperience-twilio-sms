namespace Kentico.Xperience.Twilio.SMS.Models
{
    /// <summary>
    /// Twilio SMS integration options.
    /// </summary>
    public sealed class TwilioSmsOptions
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public const string SECTION_NAME = "xperience.twilio.sms";


        /// <summary>
        /// The Twilio account ID.
        /// </summary>
        public string AccountSid
        {
            get;
            set;
        }


        /// <summary>
        /// The Twilio account authorization token.
        /// </summary>
        public string AuthToken
        {
            get;
            set;
        }
    }
}
