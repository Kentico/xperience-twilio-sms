using CMS;
using CMS.Core;

using Kentico.Xperience.Twilio.SMS.Models;
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
        private readonly IEventLogService eventLogService;
        private readonly ILocalizationService localizationService;
        private readonly ISettingsService settingsService;
        private readonly Regex phoneNumberRegex = new("^\\+[1-9]\\d{1,14}$");
        private readonly Dictionary<string, NumberValidationResponse> validatedNumbers = new();


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
        public TwilioSmsClient(IEventLogService eventLogService,
            ILocalizationService localizationService,
            ISettingsService settingsService)
        {
            this.eventLogService = eventLogService;
            this.localizationService = localizationService;
            this.settingsService = settingsService;
        }


        /// <inheritdoc/>
        public Task<MessagingResponse> SendMessageAsync(CreateMessageOptions options)
        {
            if (options.From == null && String.IsNullOrEmpty(options.MessagingServiceSid))
            {
                options.MessagingServiceSid = MessagingServiceSid;
            }

            var error = ValidateOptions(options);
            if (!String.IsNullOrEmpty(error))
            {
                return Task.FromResult(HandleSendError(error));
            }

            return SendMessageAsyncInternal(options);
        }


        /// <inheritdoc/>
        public Task<NumberValidationResponse> ValidatePhoneNumberAsync(string phoneNumber, string countryCode = null)
        {
            if (String.IsNullOrEmpty(phoneNumber))
            {
                return Task.FromResult(HandleValidationError(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyValidationNumber")));
            }

            if (!TwilioSmsModule.TwilioClientInitialized)
            {
                return Task.FromResult(HandleValidationError(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.ClientNotInitialized")));
            }

            return ValidatePhoneNumberAsyncInternal(phoneNumber, countryCode);
        }


        private MessagingResponse HandleSendError(string error)
        {
            eventLogService.LogError(nameof(TwilioSmsClient), nameof(HandleSendError), error);
            return new MessagingResponse(false)
            {
                ErrorMessage = error
            };
        }


        private NumberValidationResponse HandleValidationError(string error)
        {
            eventLogService.LogError(nameof(TwilioSmsClient), nameof(HandleValidationError), error);
            return new NumberValidationResponse(false)
            {
                ErrorMessage = error
            };
        }


        private bool NumberIsValid(PhoneNumber phoneNumber)
        {
            return phoneNumberRegex.IsMatch(phoneNumber.ToString());
        }


        private async Task<MessagingResponse> SendMessageAsyncInternal(CreateMessageOptions options)
        {
            try
            {
                var response = await MessageResource.CreateAsync(options);

                return new MessagingResponse(true)
                {
                    Id = response.Sid,
                    Status = response.Status,
                    ErrorMessage = response.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                return HandleSendError(ex.Message);
            }
        }


        private string ValidateOptions(CreateMessageOptions options)
        {
            if (!TwilioSmsModule.TwilioClientInitialized)
            {
                return localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.ClientNotInitialized");
            }

            if (options.From == null && String.IsNullOrEmpty(options.MessagingServiceSid))
            {
                return localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptySender");
            }

            if (options.From != null && !NumberIsValid(options.From))
            {
                return String.Format(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.InvalidNumber"), options.From);
            }

            if (String.IsNullOrEmpty(options.Body))
            {
                return localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyBody");
            }

            if (options.To == null)
            {
                return localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyRecipient");
            }

            if (!NumberIsValid(options.To))
            {
                return String.Format(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.InvalidNumber"), options.To);
            }

            return String.Empty;
        }


        private async Task<NumberValidationResponse> ValidatePhoneNumberAsyncInternal(string phoneNumber, string countryCode)
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

            try
            {
                var response = await PhoneNumberResource.FetchAsync(options);
                var validationResponse = new NumberValidationResponse(true)
                {
                    Valid = response.Valid,
                    FormattedNumber = response.PhoneNumber?.ToString(),
                    NationalFormat = response.NationalFormat,
                    CountryCode = response.CountryCode,
                    ValidationErrors = response.ValidationErrors
                };

                validatedNumbers.Add(cacheKey, validationResponse);

                return validationResponse;
            }
            catch (Exception ex)
            {
                return HandleValidationError(ex.Message);
            }
        }
    }
}
