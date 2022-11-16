using Kentico.Xperience.Twilio.SMS.Models;

using System.Threading.Tasks;

using Twilio.Rest.Api.V2010.Account;

namespace Kentico.Xperience.Twilio.SMS.Services
{
    /// <summary>
    /// Contains methods for interfacing with the Twilio SMS API.
    /// </summary>
    public interface ITwilioSmsClient
    {
        /// <summary>
        /// Sends an SMS message using the provided <paramref name="options"/>.
        /// </summary>
        /// <remarks>If no <see cref="CreateMessageOptions.From"/> or <see cref="CreateMessageOptions.MessagingServiceSid"/> is set,
        /// the default Messaging Service from the Xperience settings will be used.</remarks>
        /// <param name="options">The options to use.</param>
        /// <returns>The response from the Twilio API.</returns>
        Task<MessagingResponse> SendMessageAsync(CreateMessageOptions options);


        /// <summary>
        /// Validates the provided <paramref name="phoneNumber"/> with Twilio's API to convert a potentially invalid number into
        /// one that is accepted by Twilio's services.
        /// </summary>
        /// <param name="phoneNumber">The number to validate.</param>
        /// <param name="countryCode">Country code for national phone number lookups. If not provided, the number is verified with
        /// Twilio's international lookup service.</param>
        /// <returns>The response from the Twilio API.</returns>
        Task<NumberValidationResponse> ValidatePhoneNumberAsync(string phoneNumber, string countryCode = null);
    }
}
