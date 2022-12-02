using CMS.ContactManagement;
using CMS.Core;
using CMS.EventLog;

using Kentico.Xperience.Twilio.SMS.Services;

using System;

using Twilio.Rest.Api.V2010.Account;

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
                LogMessage(EventType.ERROR, nameof(SendMessageAutomationAction), "The SMS text cannot be empty.", Contact);
                return;
            }

            var recipientColumnName = GetResolvedParameter("Recipient", String.Empty);
            if (String.IsNullOrEmpty(recipientColumnName))
            {
                LogMessage(EventType.ERROR, nameof(SendMessageAutomationAction), "No contact phone number field selected.", Contact);
                return;
            }

            var recipientNumber = Contact.GetStringValue(recipientColumnName, String.Empty);
            if (String.IsNullOrEmpty(recipientNumber))
            {
                LogMessage(EventType.ERROR, nameof(SendMessageAutomationAction), $"No phone number found in contact field '{recipientColumnName}.'", Contact);
                return;
            }

            var options = new CreateMessageOptions(recipientNumber) {
                Body = message
            };

            try
            {
                Service.Resolve<ITwilioSmsClient>().SendMessageAsync(options).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogMessage(EventType.ERROR, nameof(SendMessageAutomationAction), ex.Message, Contact);
            }
        }
    }
}