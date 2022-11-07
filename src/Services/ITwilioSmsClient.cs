using Kentico.Xperience.Twilio.SMS.Models;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kentico.Xperience.Twilio.SMS.Services
{
    /// <summary>
    /// Contains methods for dispatching SMS messages to Twilio for processing.
    /// </summary>
    public interface ITwilioSmsClient
    {
        /// <summary>
        /// Sends an SMS message from the Twilio Messaging Service stored in the Xperience settings.
        /// </summary>
        /// <param name="message">The body of the SMS message.</param>
        /// <param name="recipientNumber">The phone number to send the message to.</param>
        /// <param name="messagingServiceSid">If provided, the specified Messaging Service will be used instead of
        /// the default Messaging Service.</param>
        /// <param name="mediaUrls">A list of absolute URLs to media files included in the message.</param>
        /// <returns>The response from the Twilio API.</returns>
        Task<MessagingResponse> SendMessageFromService(string message, string recipientNumber, string messagingServiceSid = null, IEnumerable<string> mediaUrls = null);


        /// <summary>
        /// Sends an SMS message from a Twilio phone number.
        /// </summary>
        /// <param name="message">The body of the SMS message.</param>
        /// <param name="recipientNumber">The phone number to send the message to.</param>
        /// <param name="fromNumber">The number to send the message from.</param>
        /// <param name="mediaUrls">A list of absolute URLs to media files included in the message.</param>
        /// <returns>The response from the Twilio API.</returns>
        Task<MessagingResponse> SendMessageFromNumber(string message, string recipientNumber, string fromNumber, IEnumerable<string> mediaUrls = null);


        /// <summary>
        /// Validates the provided <paramref name="phoneNumber"/> with Twilio's API to convert a potentially invalid number into
        /// one that is accepted by Twilio's services.
        /// </summary>
        /// <param name="phoneNumber">The number to validate.</param>
        /// <param name="countryCode">Country code for national phone number lookups. If not provided, the number is verified with
        /// Twilio's international lookup service.</param>
        /// <returns>The response from the Twilio API.</returns>
        Task<NumberValidationResponse> ValidatePhoneNumber(string phoneNumber, string countryCode = null);
    }
}
