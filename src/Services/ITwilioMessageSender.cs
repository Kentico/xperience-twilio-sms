using Kentico.Xperience.Twilio.SMS.Models;

namespace Kentico.Xperience.Twilio.SMS.Services
{
    /// <summary>
    /// Contains methods for dispatching SMS messages to Twilio for processing.
    /// </summary>
    public interface ITwilioMessageSender
    {
        /// <summary>
        /// Sends an SMS message from a Twilio Messaging Service.
        /// </summary>
        /// <param name="message">The body of the SMS message.</param>
        /// <param name="recipientNumber">The phone number to send the message to.</param>
        /// <param name="messagingServiceSid">If provided, the specified Messaging Service will be used instead of
        /// the default Messaging Service.</param>
        /// <returns>The response from the Twilio API.</returns>
        MessagingResponse SendMessageFromService(string message, string recipientNumber, string messagingServiceSid = null);


        /// <summary>
        /// Sends an SMS message from a Twilio phone number.
        /// </summary>
        /// <param name="message">The body of the SMS message.</param>
        /// <param name="recipientNumber">The phone number to send the message to.</param>
        /// <param name="fromNumber">The number to send the message from.</param>
        /// <returns>The response from the Twilio API.</returns>
        MessagingResponse SendMessageFromNumber(string message, string recipientNumber, string fromNumber);
    }
}
