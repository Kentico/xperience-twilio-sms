using CMS;
using CMS.Core;

using Kentico.Xperience.Twilio.SMS.Models;
using Kentico.Xperience.Twilio.SMS.Services;

using System;
using System.Text.RegularExpressions;

using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

[assembly: RegisterImplementation(typeof(ITwilioMessageSender), typeof(DefaultTwilioMessageSender), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.Twilio.SMS.Services
{
    internal class DefaultTwilioMessageSender : ITwilioMessageSender
    {
        private const string SETTING_TWILIO_MESSAGINGSERVICESID = "TwilioSMSMessagingService";
        private readonly IAppSettingsService appSettingsService;
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


        public DefaultTwilioMessageSender(IAppSettingsService appSettingsService,
            IEventLogService eventLogService,
            ILocalizationService localizationService,
            ISettingsService settingsService)
        {
            this.appSettingsService = appSettingsService;
            this.eventLogService = eventLogService;
            this.localizationService = localizationService;
            this.settingsService = settingsService;
        }


        public MessagingResponse SendMessageFromNumber(string message, string recipientNumber, string fromNumber)
        {
            var errorResponse = ValidateCommonParameters(message, recipientNumber);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            if (String.IsNullOrEmpty(fromNumber))
            {
                return HandleSendError(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptySender"));
            }

            if (!NumberIsValid(fromNumber))
            {
                return HandleSendError(String.Format(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.InvalidNumber"), fromNumber));
            }

            return SendMessageFromNumberInternal(message, recipientNumber, fromNumber);
        }


        public MessagingResponse SendMessageFromService(string message, string recipientNumber, string messagingServiceSid = null)
        {
            var errorResponse = ValidateCommonParameters(message, recipientNumber);
            if (errorResponse != null)
            {
                return errorResponse;
            }

            var messagingService = String.IsNullOrEmpty(messagingServiceSid) ? MessagingServiceSid : messagingServiceSid;
            if (String.IsNullOrEmpty(messagingService))
            {
                return HandleSendError(localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyServiceID"));
            }

            return SendMessageFromServiceInternal(message, recipientNumber, messagingService);
        }


        private bool NumberIsValid(string phoneNumber)
        {
            return phoneNumberRegex.IsMatch(phoneNumber);
        }


        private MessagingResponse SendMessageFromNumberInternal(string message, string recipientNumber, string fromNumber)
        {
            try
            {
                var response = MessageResource.Create(
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


        private MessagingResponse SendMessageFromServiceInternal(string message, string recipientNumber, string messagingService)
        {
            try
            {
                var response = MessageResource.Create(
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


        private MessagingResponse HandleSendError(string error)
        {
            eventLogService.LogError(nameof(DefaultTwilioMessageSender), nameof(HandleSendError), error);
            return new MessagingResponse(MessageResource.StatusEnum.Failed)
            {
                ErrorMessage = error
            };
        }


        private MessagingResponse ValidateCommonParameters(string message, string recipientNumber)
        {
            if (!TwilioSMSModule.TwilioClientInitialized)
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
    }
}
