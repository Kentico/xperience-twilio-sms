using CMS;
using CMS.Core;

using Kentico.Xperience.Twilio.SMS.Models;
using Kentico.Xperience.Twilio.SMS.Services;

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Lookups.V2;
using Twilio.Types;

[assembly: RegisterImplementation(typeof(ITwilioMessageSender), typeof(DefaultTwilioMessageSender), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.Twilio.SMS.Services
{
    internal class DefaultTwilioMessageSender : ITwilioMessageSender
    {
        private const string SETTING_TWILIO_MESSAGINGSERVICESID = "TwilioSMSMessagingService";
        private readonly IEventLogService eventLogService;
        private readonly ILocalizationService localizationService;
        private readonly ISettingsService settingsService;
        private readonly Regex phoneNumberRegex = new("^\\+[1-9]\\d{1,14}$");


        private string MessagingServiceSid
        {
            get
            {
                return settingsService[SETTING_TWILIO_MESSAGINGSERVICESID];
            }
        }


        public DefaultTwilioMessageSender(IEventLogService eventLogService,
            ILocalizationService localizationService,
            ISettingsService settingsService)
        {
            this.eventLogService = eventLogService;
            this.localizationService = localizationService;
            this.settingsService = settingsService;
        }


        public Task<MessagingResponse> SendMessageFromNumber(string message, string recipientNumber, string fromNumber)
        {
            var errorResponse = ValidateCommonSendParameters(message, recipientNumber);
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

            return SendMessageFromNumberInternal(message, recipientNumber, fromNumber);
        }


        public Task<MessagingResponse> SendMessageFromService(string message, string recipientNumber, string messagingServiceSid = null)
        {
            var errorResponse = ValidateCommonSendParameters(message, recipientNumber);
            if (errorResponse != null)
            {
                return Task.FromResult(errorResponse);
            }

            var messagingService = String.IsNullOrEmpty(messagingServiceSid) ? MessagingServiceSid : messagingServiceSid;
            if (String.IsNullOrEmpty(messagingService))
            {
                return Task.FromResult(HandleSendError(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyServiceID")));
            }

            return SendMessageFromServiceInternal(message, recipientNumber, messagingService);
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
            eventLogService.LogError(nameof(DefaultTwilioMessageSender), nameof(HandleSendError), error);
            return new MessagingResponse(MessageResource.StatusEnum.Failed)
            {
                ErrorMessage = error
            };
        }


        private NumberValidationResponse HandleValidationError(string error)
        {
            eventLogService.LogError(nameof(DefaultTwilioMessageSender), nameof(HandleValidationError), error);
            return new NumberValidationResponse(false)
            {
                ErrorMessage = error
            };
        }


        private bool NumberIsValid(string phoneNumber)
        {
            return phoneNumberRegex.IsMatch(phoneNumber);
        }


        private async Task<MessagingResponse> SendMessageFromNumberInternal(string message, string recipientNumber, string fromNumber)
        {
            try
            {
                var response = await MessageResource.CreateAsync(
                    new CreateMessageOptions(new PhoneNumber(recipientNumber))
                    {
                        Body = message,
                        From = fromNumber
                    }
                );

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


        private async Task<MessagingResponse> SendMessageFromServiceInternal(string message, string recipientNumber, string messagingService)
        {
            try
            {
                var response = await MessageResource.CreateAsync(
                    new CreateMessageOptions(new PhoneNumber(recipientNumber))
                    {
                        Body = message,
                        MessagingServiceSid = messagingService
                    }
                );

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


        private MessagingResponse ValidateCommonSendParameters(string message, string recipientNumber)
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

            return null;
        }


        private async Task<NumberValidationResponse> ValidatePhoneNumberInternal(string phoneNumber, string countryCode)
        {
            var options = new FetchPhoneNumberOptions(phoneNumber);
            if (!String.IsNullOrEmpty(countryCode))
            {
                options.CountryCode = countryCode;
            }

            try
            {
                var response = await PhoneNumberResource.FetchAsync(options);
                return new NumberValidationResponse(true)
                {
                    Valid = response.Valid,
                    FormattedNumber = response.PhoneNumber?.ToString(),
                    NationalFormat = response.NationalFormat,
                    CountryCode = response.CountryCode,
                    ValidationErrors = response.ValidationErrors
                };
            }
            catch (Exception ex)
            {
                return HandleValidationError(ex.Message);
            }
        }
    }
}
