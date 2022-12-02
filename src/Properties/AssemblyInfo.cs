using CMS;
using CMS.Core;
using CMS.MacroEngine;

using Kentico.Xperience.Twilio.SMS;
using Kentico.Xperience.Twilio.SMS.Services;

using System.Runtime.CompilerServices;

[assembly: AssemblyDiscoverable]
[assembly: RegisterModule(typeof(TwilioSmsModule))]
[assembly: InternalsVisibleTo("Kentico.Xperience.Twilio.SMS.Tests")]
[assembly: RegisterExtension(typeof(TwilioSmsMacros), typeof(UtilNamespace))]
[assembly: RegisterImplementation(typeof(ITwilioSmsClient), typeof(TwilioSmsClient), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
