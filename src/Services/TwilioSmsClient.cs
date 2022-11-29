using CMS;
using CMS.Core;

using Kentico.Xperience.Twilio.SMS.Services;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Lookups.V2;
using Twilio.Types;

[assembly: RegisterImplementation(typeof(ITwilioSmsClient), typeof(TwilioSmsClient), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.Twilio.SMS.Services
{
    /// <summary>
    /// Default implementation of <see cref="ITwilioSmsClient"/>.
    /// </summary>
    internal class TwilioSmsClient : ITwilioSmsClient
    {
        private const string SETTING_TWILIO_MESSAGINGSERVICESID = "TwilioSMSMessagingService";
        private readonly ISettingsService settingsService;
        private readonly Regex countryCodeRegex = new("^[A-Z]{2}$");
        private readonly Regex phoneNumberRegex = new("^\\+[1-9]\\d{1,14}$");
        private readonly Dictionary<string, PhoneNumberResource> validatedNumbers = new();


        private string MessagingServiceSid
        {
            get
            {
                return settingsService[SETTING_TWILIO_MESSAGINGSERVICESID];
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="TwilioSmsClient"/> class.
        /// </summary>
        public TwilioSmsClient(ISettingsService settingsService)
        {
            this.settingsService = settingsService;
        }


        /// <inheritdoc/>
        public async Task<MessageResource> SendMessageAsync(CreateMessageOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (String.IsNullOrEmpty(options.Body))
            {
                throw new InvalidOperationException("Message body cannot be empty.");
            }

            if (options.To == null)
            {
                throw new InvalidOperationException("Recipient phone number cannot be empty.");
            }

            if (options.From != null && !NumberIsValid(options.From))
            {
                throw new InvalidOperationException($"The number '{options.From}' is not in a valid Twilio format.");
            }

            if (!TwilioSmsModule.TwilioClientInitialized)
            {
                throw new InvalidOperationException("The Twilio client is not initialized. Please check your application settings.");
            }

            if (options.From == null && String.IsNullOrEmpty(options.MessagingServiceSid))
            {
                // Set Messaging Service from settings if From number or service not set in options
                if (String.IsNullOrEmpty(MessagingServiceSid))
                {
                    throw new InvalidOperationException("No 'From' phone number or Messaging Service provided.");
                }
                else
                {
                    options.MessagingServiceSid = MessagingServiceSid;
                }
            }

            if (!NumberIsValid(options.To))
            {
                throw new InvalidOperationException($"The number '{options.To}' is not in a valid Twilio format.");
            }

            return await MessageResource.CreateAsync(options);
        }


        /// <inheritdoc/>
        public Task<PhoneNumberResource> ValidatePhoneNumberAsync(string phoneNumber, string countryCode = null)
        {
            if (String.IsNullOrEmpty(phoneNumber))
            {
                throw new ArgumentNullException(nameof(phoneNumber));
            }

            if (!TwilioSmsModule.TwilioClientInitialized)
            {
                throw new InvalidOperationException("The Twilio client is not initialized. Please check your application settings.");
            }

            if (!String.IsNullOrEmpty(countryCode) && !countryCodeRegex.IsMatch(countryCode))
            {
                throw new ArgumentException($"The country code '{countryCode}' isn't a valid 2-letter country code.");
            }

            return ValidatePhoneNumberAsyncInternal(phoneNumber, countryCode);
        }


        private bool NumberIsValid(PhoneNumber phoneNumber)
        {
            return phoneNumberRegex.IsMatch(phoneNumber.ToString());
        }


        private async Task<PhoneNumberResource> ValidatePhoneNumberAsyncInternal(string phoneNumber, string countryCode)
        {
            var cacheKey = $"{phoneNumber}|{countryCode}";
            if (validatedNumbers.TryGetValue(cacheKey, out var cachedResponse))
            {
                return cachedResponse;
            }

            var options = new FetchPhoneNumberOptions(phoneNumber);
            if (!String.IsNullOrEmpty(countryCode))
            {
                options.CountryCode = countryCode;
            }

            var response = await PhoneNumberResource.FetchAsync(options);
            validatedNumbers.Add(cacheKey, response);

            return response;
        }
    }
}
