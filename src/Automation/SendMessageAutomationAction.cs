using CMS.ContactManagement;
using CMS.Core;

using Kentico.Xperience.Twilio.SMS.Services;

using System;

namespace Kentico.Xperience.Twilio.SMS
{
    internal class SendMessageAutomationAction : ContactAutomationAction
    {
        public override void Execute()
        {
            var message = GetResolvedParameter<string>("Text", String.Empty);
            if (String.IsNullOrEmpty(message))
            {
                throw new InvalidOperationException("SMS body text cannot be empty.");
            }

            var recipientColumnName = GetResolvedParameter<string>("Recipient", String.Empty);
            if (String.IsNullOrEmpty(recipientColumnName))
            {
                throw new InvalidOperationException("Recipient phone number column not selected.");
            }

            var recipientNumber = Contact.GetStringValue(recipientColumnName, String.Empty);
            if (String.IsNullOrEmpty(recipientNumber))
            {
                throw new InvalidOperationException("Recipient phone number cannot be empty.");
            }

            Service.Resolve<ITwilioMessageSender>().SendMessageFromService(message, recipientNumber);
        }
    }
}
