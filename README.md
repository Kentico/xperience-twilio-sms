[![Stack Overflow](https://img.shields.io/badge/Stack%20Overflow-ASK%20NOW-FE7A16.svg?logo=stackoverflow&logoColor=white)](https://stackoverflow.com/tags/kentico)
![Kentico.Xperience.Libraries 13.0.73](https://img.shields.io/badge/Kentico.Xperience.Libraries-v13.0.73-orange)
[![Nuget](https://img.shields.io/nuget/v/Kentico.Xperience.Twilio.SMS)](https://www.nuget.org/packages/Kentico.Xperience.Twilio.SMS)

# Twilio SMS Kentico Xperience 13 integration

This integration contains custom [Marketing automation](https://docs.xperience.io/on-line-marketing-features/managing-your-on-line-marketing-features/marketing-automation) actions which allow you to send SMS messages to your Xperience contacts easily, without the need for coding! It also exposes some Twilio API methods in an easy-to-use asynchronous service, available in both the CMS and front-end application, allowing your developers to implement more advanced scenarios.

## Getting Started

### Prerequisites

- This integration requires a Kentico Xperience installation on version __13.0.73__ or higher.
- You will need a Twilio account, which you can sign up for free here: https://www.twilio.com/try-twilio. Once you are registered, sign in to the [Twilio Console](https://console.twilio.com/), click __Account > API keys & tokens__, and note the __Account SID__ and __Auth token__ values in the "Live credentials" section.
- You must create one or more [Messaging Services](https://support.twilio.com/hc/en-us/articles/223181308-Getting-started-with-Messaging-Services) to send SMS messages via the [Marketing automation](#marketing-automation) action.
- To send SMS messages with Twilio, your contact phone numbers must be in the [E.164](https://www.twilio.com/docs/glossary/what-e164) format. See the [Formatting contact phone numbers](#formatting-contact-phone-numbers) section for more information.

### Administration project installation

1. Install the [Kentico.Xperience.Twilio.SMS](https://www.nuget.org/packages/Kentico.Xperience.Twilio.SMS) NuGet package in the CMS administration project.
2. In your administration project's `web.config` files's `appSettings` section, add your API keys noted in the [Prerequisites](#prerequisites) section:

```xml
<add key="TwilioAuthToken" value="<Auth token>"/>
<add key="TwilioAccountSID" value="<Account SID>"/>
```

3. Download the _Kentico.Xperience.Twilio.SMS_ ZIP package by locating the latest [Release](https://github.com/Kentico/xperience-google-datastudio/releases).
4. In the Xperience administration, open the **Sites** application.
5. [Import](https://docs.xperience.io/x/VAeRBg) the downloaded package.
    * Make sure the **Import files** and **Import code files** [settings](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects#Importingasiteorobjects-Import-Objectselectionsettings) are enabled.
6. Perform the [necessary steps](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects#Importingasiteorobjects-Importingpackageswithfiles) to include the following imported folder in your project:
   * `/CMSModules/Kentico.Xperience.Twilio.SMS`

### Live-site project installation (optional)

Installation of the integration on the live-site is not required to use the custom Marketing automation actions included. However, if you would like to use [the API](#using-the-api) on the live-site, you can install it by following these instructions:

1. Install the [Kentico.Xperience.Twilio.SMS](https://www.nuget.org/packages/Kentico.Xperience.Twilio.SMS) NuGet package in the live-site project.
2. In the live-site project's `appsettings.json`, add the following section with your API keys noted in the [Prerequisites](#prerequisites) section:

```json
"xperience.twilio.sms": {
  "accountSid": "<Account SID>",
  "authToken": "<Auth token>"
}
```

3. In `Startup.cs`, register the integration in the `ConfigureServices()` method:

```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddTwilioSms(Configuration);
```

## Marketing automation

This integration includes a new [Marketing automation](https://docs.xperience.io/on-line-marketing-features/managing-your-on-line-marketing-features/marketing-automation) action which can be added to your processes. The "Send Twilio SMS" Marketing automation action will send an SMS to the contact using the provided phone number and text. To begin using this action, you _must_ select a Messaging Service to use for sending- see [Prerequisites](#prerequisites) for more information on creating a Messaging Service. To select the Messaging Service, open the Xperience administration and go to the __Settings__ application. In the __Integration > Twilio SMS__ section, you will find a new setting called "Messaging service." Use the drop-down list to select the default Messaging Service, and click __Save__.

After adding the "Send Twilio SMS" action to an automation process, select the contact's phone number and enter the SMS text to send. You can click the black arrow next to the "Text" property to enter macros, such as "{%Contact.ContactFirstName%}."

![Send SMS message](/img/send-sms-properties.png)

When the action executes, the SMS will be sent to the contact using the Messaging Service selected in the __Settings__ application. 


## Formatting contact phone numbers

Twilio requires recipient phone numbers in the [E.164](https://www.twilio.com/docs/glossary/what-e164) format, for example "+12223334444." If you have been storing contact phone numbers in your Kentico Xperience website, you are most likely using the built-in "Private phone" or "Business phone" fields, which are plain text fields with no formatting. To ensure that your SMS messages can be sent to contacts, you can use the `ValidatePhoneNumberAsync()` method from [the API](#validating-phone-numbers) format the contact's phone number into E.164.

For example, you can create an [object event handler](https://docs.xperience.io/custom-development/handling-global-events/handling-object-events) to format phone numbers whenever a contact is updated:

```cs
[assembly: RegisterModule(typeof(MySite.CustomModule))]
namespace MySite
{
    public class CustomModule : Module
    {
        private ITwilioSmsClient twilioSmsClient;
        private readonly Regex phoneNumberRegex = new Regex("^\\+[1-9]\\d{1,14}$");
        // Add any contact fields you'd like to validate here
        private readonly string[] phoneNumberColumns = new string[]
        {
            nameof(ContactInfo.ContactBusinessPhone),
            nameof(ContactInfo.ContactMobilePhone),
            "CustomPhoneColumn"
        };

        public CustomModule() : base("MyCustomModule")
        {
        }

        protected override void OnInit()
        {
            base.OnInit();

            twilioSmsClient = Service.Resolve<ITwilioSmsClient>();
            ContactInfo.TYPEINFO.Events.Insert.After += UpdateContactNumbers;
            ContactInfo.TYPEINFO.Events.Update.After += UpdateContactNumbers;
        }

        private void UpdateContactNumbers(object sender, ObjectEventArgs e)
        {
            var contact = e.Object as ContactInfo;
            var countryId = ValidationHelper.GetInteger(contact.ContactCountryID, 0);
            string countryCode = null;
            if (countryId > 0)
            {
                var contactCountry = CountryInfo.Provider.Get(countryId);
                if (contactCountry != null)
                {
                    countryCode = contactCountry.CountryTwoLetterCode;
                }
            }

            foreach (var column in phoneNumberColumns)
            {
                Task.Run(() => UpdateNumber(column, contact, countryCode));
            }
        }

        private async Task UpdateNumber(string column, ContactInfo contact, string countryCode)
        {
            var phoneNumber = contact.GetStringValue(column, String.Empty);
            if (String.IsNullOrEmpty(phoneNumber) || phoneNumberRegex.IsMatch(phoneNumber))
            {
                return;
            }

            try 
            {
                var validationResponse = await twilioSmsClient.ValidatePhoneNumberAsync(phoneNumber, countryCode);
                if (validationResponse.Valid ?? false && validationResult.PhoneNumber != null)
                {
                    contact.SetValue(column, validationResponse.PhoneNumber.ToString());
                    contact.Update();
                }
            }
            catch (Exception ex)
            {
                // Log error...
            }
        }
    }
}
```

## Using the API

This integration provides the [`ITwilioSmsClient`](/src/Services/ITwilioSmsClient.cs) which can be used within both the administration and live-site applications to develop advanced functionality. The client provides the following methods:

- `SendMessageAsync`: Sends an SMS message to a recipient. If a "From" phone number or Messaging Service isn't provided in the parameters, the Messaging Service selected in the __Settings__ application is used to send the SMS.
- `ValidatePhoneNumberAsync`: Requests a valid [E.164](https://www.twilio.com/docs/glossary/what-e164) phone number from Twilio's lookup service.

### Sending SMS messages

The `SendMessageAsync()` method accepts an `CreateMessageOptions` object as a parameter, which should include at least the following properties:

- __To__: Set in the constructor, this is the phone number of the SMS recipient.
- __Body__: The text of the SMS message.

You can provide a __From__ phone number or __MessagingServiceSid__ to customize how the SMS is sent. If not provided, the Messaging Service chosen in __Settings application > Integration > Twilio SMS__ is used. The returned [`MessageResource`](https://www.twilio.com/docs/sms/api/message-resource) contains a `Status` property indicating whether Twilio successfully processed by Twilio- otherwise it will be `Failed`.

```cs
var options = new CreateMessageOptions(phoneNumber)
{
    Body = "Thank you for registering. Please verify your account by clicking this link: https://yoursite.com/verify"
};
var result = await twilioSmsClient.SendMessageAsync(options);
if (result.Status == MessageResource.StatusEnum.Failed)
{
    ShowError(result.ErrorMessage);
}
```

### Validating phone numbers

The `ValidatePhoneNumberAsync()` method attempts to retrieve a valid [E.164](https://www.twilio.com/docs/glossary/what-e164) phone number from Twilio's lookup service. The `countryCode` parameter should be a valid [2-letter country code](https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2#Officially_assigned_code_elements) such as "US." If provided, a [national lookup](https://www.twilio.com/docs/lookup/tutorials/validation-and-formatting#validate-a-national-phone-number) is performed. Otherwise, the [international lookup](https://www.twilio.com/docs/lookup/tutorials/validation-and-formatting#format-an-international-phone-number) is used.

The method returns a [`PhoneNumberResource`](https://www.twilio.com/docs/lookup/tutorials/validation-and-formatting#validate-a-national-phone-number) which indicates whether the request was successful and a valid number was found. First, you should check `Valid` to see if a valid E.164 phone number was found. Then, ensure that the returned `PhoneNumber` is not `null`. The following sample code can be used to verify whether you can use the returned number:

```cs
var hasValidNumber = validationResponse.Valid ?? false && validationResponse.PhoneNumber != null;
```

### Example: Sending multi-factor authentication passcodes via SMS

By default, if you have enabled [multi-factor authentication](https://docs.xperience.io/managing-users/user-registration-and-authentication/configuring-multi-factor-authentication) for Kentico Xperience, your users must register a TOTP authenticator like [Google Authenticator](https://support.google.com/accounts/answer/1066447?hl=en). However, it would be more convenient and faster if the passcode were delivered straight to their phone via SMS. Fortunately, it is easy to override the authentication process by creating a custom module, as described in [our documentation](https://docs.xperience.io/custom-development/handling-global-events/handling-custom-multi-factor-authentication).

Our sample code illustrates how to email the passcode to your users, so we can replace the email sending code with code from `ITwilioSmsClient`. To ensure the SMS is sent properly, you may want to validate the number using the `ValidatePhoneNumberAsync()` method, and send the passcode via email if the SMS cannot be sent:

```cs
[assembly: RegisterModule(typeof(MySite.CustomModule))]
namespace MySite
{
    public class CustomModule : Module
    {
        private ITwilioSmsClient twilioSmsClient;
        private readonly Regex phoneNumberRegex = new Regex("^\\+[1-9]\\d{1,14}$");

        public CustomModule() : base("MyCustomModule")
        {
        }

        protected override void OnInit()
        {
            base.OnInit();

            twilioSmsClient = Service.Resolve<ITwilioSmsClient>();
            SecurityEvents.MultiFactorAuthenticate.Execute += MFAuthentication_Execute;
        }

        private void MFAuthentication_Execute(object sender, AuthenticationEventArgs e)
        {
            Task.Run(() => SendPasscodeSMS(e.User, e.Passcode));
        }

        private async Task SendPasscodeSMS(UserInfo user, string passcode)
        {
            var phoneNumber = user.UserSettings.UserPhone;
            if (String.IsNullOrEmpty(phoneNumber))
            {
                // User has no phone number
                SendPasscodeEmail(user, passcode);
                return;
            }

            if (!phoneNumberRegex.IsMatch(phoneNumber))
            {
                var validationResult = await twilioSmsClient.ValidatePhoneNumberAsync(phoneNumber);
                if (validationResult.Valid ?? false && validationResult.PhoneNumber != null)
                {
                    // Request found E.164 phone number, update user
                    phoneNumber = validationResult.PhoneNumber.ToString();
                    user.UserSettings.UserPhone = phoneNumber;
                    user.Update();
                }
                else
                {
                    // Request failed or couldn't find a valid phone number
                    SendPasscodeEmail(user, passcode);
                    return;
                }
            }

            var options = new CreateMessageOptions(phoneNumber)
            {
                Body = $"Your Kentico Xperience passcode is: {passcode}"
            };
            var smsResult = await twilioSmsClient.SendMessageAsync(options);
            if (smsResult.Status == MessageResource.StatusEnum.Failed)
            {
                // SMS send failed, fallback to email
                SendPasscodeEmail(user, passcode);
            }
        }
    }
}
```

## Contributing

For Contributing please see  <a href="./CONTRIBUTING.md">`CONTRIBUTING.md`</a> for more information.

## License

Distributed under the MIT License. See [`LICENSE.md`](./LICENSE.md) for more information.