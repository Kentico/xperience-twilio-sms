using CMS.ContactManagement;
using CMS.Core;
using CMS.EventLog;
using CMS.Globalization;
using CMS.Helpers;

using Kentico.Xperience.Twilio.SMS.Services;

using System;
using System.Text.RegularExpressions;

namespace Kentico.Xperience.Twilio.SMS
{
    /// <summary>
    /// A Marketing Automation action that uses Twilio API to validate the contact's stored phone number and automatically updates it.
    /// </summary>
    public sealed class UpdatePhoneNumberAutomationAction : ContactAutomationAction
    {
        private readonly Regex phoneNumberRegex = new("^\\+[1-9]\\d{1,14}$");


        /// <inheritdoc/>
        public override void Execute()
        {
            var numberColumnName = GetResolvedParameter("PhoneNumber", String.Empty);
            if (String.IsNullOrEmpty(numberColumnName))
            {
                LogMessage(EventType.ERROR, nameof(UpdatePhoneNumberAutomationAction), ResHelper.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyRecipient"), Contact);
                return;
            }

            var phoneNumber = Contact.GetStringValue(numberColumnName, String.Empty);
            if (phoneNumberRegex.IsMatch(phoneNumber))
            {
                return;
            }

            string countryCode = null;
            var countryId = ValidationHelper.GetInteger(Contact.ContactCountryID, 0);
            if (countryId > 0)
            {
                var contactCountry = CountryInfo.Provider.Get(countryId);
                if (contactCountry != null)
                {
                    countryCode = contactCountry.CountryTwoLetterCode;
                }
            }

            var validationResponse = Service.Resolve<ITwilioSmsClient>().ValidatePhoneNumber(phoneNumber, countryCode).ConfigureAwait(false).GetAwaiter().GetResult();
            if (validationResponse.Success && (validationResponse.Valid ?? false) && !String.IsNullOrEmpty(validationResponse.FormattedNumber))
            {
                Contact.SetValue(numberColumnName, validationResponse.FormattedNumber);
                Contact.Update();
            }
            else
            {
                LogMessage(EventType.ERROR, nameof(UpdatePhoneNumberAutomationAction),
                    ResHelper.GetStringFormat("Kentico.Xperience.Twilio.SMS.Error.ValidationFailed", phoneNumber, validationResponse.ErrorMessage), Contact);
            }
        }
    }
}
