using Kentico.Xperience.Twilio.SMS.Services;

using Twilio.Rest.Api.V2010.Account;

namespace Kentico.Xperience.Twilio.SMS.Models
{
    /// <summary>
    /// Represents the response of a <see cref="ITwilioMessageSender.SendMessage"/> call.
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
        /// The status of the message- <see cref="MessageResource.StatusEnum.Failed"/> if there was an error.
        /// </summary>
        public MessageResource.StatusEnum Status
        {
            get;
            private set;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingResponse"/> class.
        /// </summary>
        /// <param name="status">The status of the message.</param>
        public MessagingResponse(MessageResource.StatusEnum status)
        {
            Status = status;
        }
    }
}
