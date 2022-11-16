using Twilio.Rest.Api.V2010.Account;

namespace Kentico.Xperience.Twilio.SMS.Models
{
    /// <summary>
    /// Represents the response of an SMS send.
    /// </summary>
    public sealed class MessagingResponse
    {
        /// <summary>
        /// The error that occurred during sending, or null if successful.
        /// </summary>
        public string ErrorMessage
        {
            get;
            set;
        }


        /// <summary>
        /// The ID of the created message.
        /// </summary>
        public string Id
        {
            get;
            set;
        }


        /// <summary>
        /// <c>True</c> if the SMS request was sent to Twilio.
        /// </summary>
        public bool Sent
        {
            get;
            private set;
        }


        /// <summary>
        /// The status of the message- <see cref="MessageResource.StatusEnum.Failed"/> if there was an error.
        /// </summary>
        public MessageResource.StatusEnum Status
        {
            get;
            set;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingResponse"/> class.
        /// </summary>
        /// <param name="sent"><c>True</c> if the SMS request was sent to Twilio.</param>
        public MessagingResponse(bool sent)
        {
            Sent = sent;
        }
    }
}
