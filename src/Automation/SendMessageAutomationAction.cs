using CMS.ContactManagement;
using CMS.Core;
using CMS.EventLog;
using CMS.Helpers;

using Kentico.Xperience.Twilio.SMS.Services;

using System;

namespace Kentico.Xperience.Twilio.SMS
{
    /// <summary>
    /// A Marketing Automation action which sends a Twilio SMS from a Messaging Service to the selected phone number.
    /// </summary>
    public sealed class SendMessageAutomationAction : ContactAutomationAction
    {
        /// <inheritdoc/>
        public override void Execute()
        {
            var message = GetResolvedParameter("Text", String.Empty);
            if (String.IsNullOrEmpty(message))
            {
                LogMessage(EventType.ERROR, nameof(SendMessageAutomationAction), ResHelper.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyBody"), Contact);
                return;
            }

            var recipientColumnName = GetResolvedParameter("Recipient", String.Empty);
            if (String.IsNullOrEmpty(recipientColumnName))
            {
                LogMessage(EventType.ERROR, nameof(SendMessageAutomationAction), ResHelper.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyRecipient"), Contact);
                return;
            }

            var recipientNumber = Contact.GetStringValue(recipientColumnName, String.Empty);
            if (String.IsNullOrEmpty(recipientNumber))
            {
                LogMessage(EventType.ERROR, nameof(SendMessageAutomationAction), ResHelper.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyRecipient"), Contact);
                return;
            }

            Service.Resolve<ITwilioSmsClient>().SendMessageFromService(message, recipientNumber).ConfigureAwait(false);
        }
    }
}