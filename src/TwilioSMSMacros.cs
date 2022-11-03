using CMS;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.MacroEngine;

using Kentico.Xperience.Twilio.SMS;

using System;
using System.Linq;

[assembly: RegisterExtension(typeof(TwilioSMSMacros), typeof(UtilNamespace))]
namespace Kentico.Xperience.Twilio.SMS
{
    internal class TwilioSMSMacros : MacroMethodContainer
    {
        [MacroMethod(typeof(string), "Gets a list of contact fields which may contain a phone number, in a format suitable for drop-down selection.", 0)]
        public static object GetRecipientContactFields(EvaluationContext context, params object[] parameters)
        {
            var formInfo = FormHelper.GetFormInfo(ContactInfo.OBJECT_TYPE, true);
            var systemNumberFields = formInfo.GetFields(true, true, true).Where(f =>
                f.Name.Equals(nameof(ContactInfo.ContactBusinessPhone), StringComparison.OrdinalIgnoreCase) ||
                f.Name.Equals(nameof(ContactInfo.ContactMobilePhone), StringComparison.OrdinalIgnoreCase));
            var nonSystemTextFields = formInfo.GetFields(true, true, false).Where(f => f.DataType == FieldDataType.Text);
            var options = systemNumberFields.Concat(nonSystemTextFields).Select(f => $"{f.Name};{f.Caption}");

            return String.Join("\r\n", options);
        }
    }
}
