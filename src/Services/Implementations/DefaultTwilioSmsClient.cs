using CMS;
using CMS.Core;

using Kentico.Xperience.Twilio.SMS.Models;
using Kentico.Xperience.Twilio.SMS.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Lookups.V2;
using Twilio.Types;

[assembly: RegisterImplementation(typeof(ITwilioSmsClient), typeof(DefaultTwilioSmsClient), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.Twilio.SMS.Services
{
    internal class DefaultTwilioSmsClient : ITwilioSmsClient
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


        public DefaultTwilioSmsClient(IEventLogService eventLogService,
            ILocalizationService localizationService,
            ISettingsService settingsService)
        {
            this.eventLogService = eventLogService;
            this.localizationService = localizationService;
            this.settingsService = settingsService;
        }


        public Task<MessagingResponse> SendMessageFromNumber(string message, string recipientNumber, string fromNumber, IEnumerable<string> mediaUrls = null)
        {
            var errorResponse = ValidateCommonSendParameters(message, recipientNumber, mediaUrls);
            if (errorResponse != null)
            {
                return Task.FromResult(errorResponse);
            }

            if (String.IsNullOrEmpty(fromNumber))
            {
                return Task.FromResult(HandleSendError(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptySender")));
            }

            if (!NumberIsValid(fromNumber))
            {
                return Task.FromResult(HandleSendError(String.Format(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.InvalidNumber"), fromNumber)));
            }

            var options = new CreateMessageOptions(new PhoneNumber(recipientNumber))
            {
                Body = message,
                From = fromNumber
            };
            return SendMessageInternal(options, mediaUrls);
        }


        public Task<MessagingResponse> SendMessageFromService(string message, string recipientNumber, string messagingServiceSid = null, IEnumerable<string> mediaUrls = null)
        {
            var errorResponse = ValidateCommonSendParameters(message, recipientNumber, mediaUrls);
            if (errorResponse != null)
            {
                return Task.FromResult(errorResponse);
            }

            var messagingService = String.IsNullOrEmpty(messagingServiceSid) ? MessagingServiceSid : messagingServiceSid;
            if (String.IsNullOrEmpty(messagingService))
            {
                return Task.FromResult(HandleSendError(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyServiceID")));
            }

            var options = new CreateMessageOptions(new PhoneNumber(recipientNumber))
            {
                Body = message,
                MessagingServiceSid = messagingService
            };
            return SendMessageInternal(options, mediaUrls);
        }


        public Task<NumberValidationResponse> ValidatePhoneNumber(string phoneNumber, string countryCode = null)
        {
            if (String.IsNullOrEmpty(phoneNumber))
            {
                return Task.FromResult(HandleValidationError(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyValidationNumber")));
            }

            if (!TwilioSmsModule.TwilioClientInitialized)
            {
                return Task.FromResult(HandleValidationError(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.ClientNotInitialized")));
            }

            return ValidatePhoneNumberInternal(phoneNumber, countryCode);
        }


        private MessagingResponse HandleSendError(string error)
        {
            eventLogService.LogError(nameof(DefaultTwilioSmsClient), nameof(HandleSendError), error);
            return new MessagingResponse(MessageResource.StatusEnum.Failed)
            {
                ErrorMessage = error
            };
        }


        private NumberValidationResponse HandleValidationError(string error)
        {
            eventLogService.LogError(nameof(DefaultTwilioSmsClient), nameof(HandleValidationError), error);
            return new NumberValidationResponse(false)
            {
                ErrorMessage = error
            };
        }


        private bool NumberIsValid(string phoneNumber)
        {
            return phoneNumberRegex.IsMatch(phoneNumber);
        }


        private async Task<MessagingResponse> SendMessageInternal(CreateMessageOptions options, IEnumerable<string> mediaUrls)
        {
            if (mediaUrls != null && mediaUrls.Any())
            {
                var mediaUris = mediaUrls.Select(url =>
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        return uri;
                    }

                    return null;
                }).Where(uri => uri != null);
                options.MediaUrl = mediaUris.ToList();
            }

            try
            {
                var response = await MessageResource.CreateAsync(options);

                return new MessagingResponse(response.Status)
                {
                    Id = response.Sid,
                    ErrorMessage = response.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                return HandleSendError(ex.Message);
            }
        }


        private MessagingResponse ValidateCommonSendParameters(string message, string recipientNumber, IEnumerable<string> mediaUrls)
        {
            if (!TwilioSmsModule.TwilioClientInitialized)
            {
                return HandleSendError(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.ClientNotInitialized"));
            }

            if (String.IsNullOrEmpty(message))
            {
                return HandleSendError(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyBody"));
            }

            if (String.IsNullOrEmpty(recipientNumber))
            {
                return HandleSendError(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyRecipient"));
            }

            if (!NumberIsValid(recipientNumber))
            {
                return HandleSendError(String.Format(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.InvalidNumber"), recipientNumber));
            }

            if (mediaUrls != null && mediaUrls.Any())
            {
                if (mediaUrls.Count() > 10)
                {
                    return HandleSendError(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.MediaLimitExceeded"));
                }

                foreach (var url in mediaUrls)
                {
                    if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                    {
                        return HandleSendError(String.Format(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.InvalidMedia"), url));
                    }
                }
            }

            return null;
        }


        private async Task<NumberValidationResponse> ValidatePhoneNumberInternal(string phoneNumber, string countryCode)
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
