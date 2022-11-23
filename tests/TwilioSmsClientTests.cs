using CMS.Core;

using Kentico.Xperience.Twilio.SMS.Services;

using NSubstitute;

using NUnit.Framework;

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Twilio;
using Twilio.Clients;
using Twilio.Http;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Kentico.Xperience.Twilio.SMS.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="ITwilioSmsClient"/> class.
    /// </summary>
    internal class TwilioSmsClientTests
    {
        private static ITwilioSmsClient twilioSmsClient;
        private static readonly ITwilioRestClient twilioRestClient = Substitute.For<ITwilioRestClient>();
        private static readonly ISettingsService settingsService = Substitute.For<ISettingsService>();
        private static readonly ILocalizationService localizationService = Substitute.For<ILocalizationService>();
        private static readonly IEventLogService eventLogService = Substitute.For<IEventLogService>();


        /// <summary>
        /// Unit tests for the <see cref="ITwilioSmsClient.SendMessageAsync"/> method.
        /// </summary>
        [TestFixture]
        internal class SendMessageAsyncTests
        {
            private const string ACCOUNT = "testaccount";
            private const string MESSAGING_SERVICE = "testservice";
            private const string SMS_BODY = "SMS body";
            private const string SETTINGSKEY = "TwilioSMSMessagingService";
            private const string MESSAGING_PATH = $"/Accounts/{ACCOUNT}/Messages.json";


            [SetUp]
            public void SendMessageAsyncTests_SetUp()
            {
                eventLogService.ClearReceivedCalls();
                twilioRestClient.ClearReceivedCalls();
                
                settingsService[SETTINGSKEY].Returns(MESSAGING_SERVICE);
                twilioRestClient.AccountSid.Returns(ACCOUNT);
                twilioRestClient.RequestAsync(Arg.Any<Request>()).ReturnsForAnyArgs(args =>
                    Task.FromResult(new Response(HttpStatusCode.OK, $"{{ status:'{MessageResource.StatusEnum.Queued}' }}")));

                TwilioClient.SetRestClient(twilioRestClient);
                TwilioSmsModule.TwilioClientInitialized = true;

                twilioSmsClient = new TwilioSmsClient(eventLogService, localizationService, settingsService);
            }


            [Test]
            public async Task SendMessageAsync_EmptyBody_ReturnsError()
            {
                var expectedError = "empty body";
                localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyBody").Returns(expectedError);
                var options = new CreateMessageOptions("+12223334444");
                var result = await twilioSmsClient.SendMessageAsync(options);

                Assert.That(result.Sent, Is.False);
                Assert.That(result.ErrorMessage, Is.EqualTo(expectedError));
                await twilioRestClient.DidNotReceiveWithAnyArgs().RequestAsync(Arg.Any<Request>());
                eventLogService.Received().LogEvent(Arg.Is<EventLogData>(arg => arg.EventDescription.Equals(expectedError, StringComparison.OrdinalIgnoreCase)));
            }


            [Test]
            public async Task SendMessageAsync_FromNumber_OverridesMessagingService()
            {
                var expectedSender = "+15556667777";
                var expectedRecipient = "+12223334444";
                var options = new CreateMessageOptions(expectedRecipient)
                {
                    Body = SMS_BODY,
                    From = new PhoneNumber(expectedSender)
                };
                var result = await twilioSmsClient.SendMessageAsync(options);

                Assert.That(result.Sent, Is.True);
                Assert.That(result.Status, Is.EqualTo(MessageResource.StatusEnum.Queued));
                Assert.That(result.ErrorMessage, Is.Null);
                await twilioRestClient.Received().RequestAsync(Arg.Is<Request>(arg =>
                    arg.Uri.AbsolutePath.EndsWith(MESSAGING_PATH, StringComparison.OrdinalIgnoreCase)
                 && arg.PostParams.SingleOrDefault(kvp => kvp.Key.Equals("To", StringComparison.OrdinalIgnoreCase)).Value.Equals(expectedRecipient, StringComparison.OrdinalIgnoreCase)
                 && arg.PostParams.SingleOrDefault(kvp => kvp.Key.Equals("Body", StringComparison.OrdinalIgnoreCase)).Value.Equals(SMS_BODY, StringComparison.OrdinalIgnoreCase)
                 && arg.PostParams.SingleOrDefault(kvp => kvp.Key.Equals("From", StringComparison.OrdinalIgnoreCase)).Value.Equals(expectedSender, StringComparison.OrdinalIgnoreCase)));
            }


            [Test]
            public async Task SendMessageAsync_InvalidNumber_ReturnsError()
            {
                var expectedError = "invalid number";
                localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.InvalidNumber").Returns(expectedError);
                var options = new CreateMessageOptions("1112223333")
                {
                    Body = SMS_BODY
                };
                var result = await twilioSmsClient.SendMessageAsync(options);

                Assert.That(result.Sent, Is.False);
                Assert.That(result.ErrorMessage, Is.EqualTo(expectedError));
                await twilioRestClient.DidNotReceiveWithAnyArgs().RequestAsync(Arg.Any<Request>());
                eventLogService.Received().LogEvent(Arg.Is<EventLogData>(arg => arg.EventDescription.Equals(expectedError, StringComparison.OrdinalIgnoreCase)));
            }


            [Test]
            public async Task SendMessageAsync_ValidOptions_SendsMessage()
            {
                var expectedRecipient = "+12223334444";
                var options = new CreateMessageOptions(expectedRecipient)
                {
                    Body = SMS_BODY
                };
                var result = await twilioSmsClient.SendMessageAsync(options);

                Assert.That(result.Sent, Is.True);
                Assert.That(result.Status, Is.EqualTo(MessageResource.StatusEnum.Queued));
                Assert.That(result.ErrorMessage, Is.Null);
                await twilioRestClient.Received().RequestAsync(Arg.Is<Request>(arg =>
                    arg.Uri.AbsolutePath.EndsWith(MESSAGING_PATH, StringComparison.OrdinalIgnoreCase)
                 && arg.PostParams.SingleOrDefault(kvp => kvp.Key.Equals("To", StringComparison.OrdinalIgnoreCase)).Value.Equals(expectedRecipient, StringComparison.OrdinalIgnoreCase)
                 && arg.PostParams.SingleOrDefault(kvp => kvp.Key.Equals("Body", StringComparison.OrdinalIgnoreCase)).Value.Equals(SMS_BODY, StringComparison.OrdinalIgnoreCase)
                 && arg.PostParams.SingleOrDefault(kvp => kvp.Key.Equals("MessagingServiceSid", StringComparison.OrdinalIgnoreCase)).Value.Equals(MESSAGING_SERVICE, StringComparison.OrdinalIgnoreCase)));
            }
        }


        /// <summary>
        /// Unit tests for the <see cref="ITwilioSmsClient.ValidatePhoneNumberAsync"/> method.
        /// </summary>
        [TestFixture]
        internal class ValidateNumberAsyncTests
        {
            private const string VALIDATION_PATH = "/v2/PhoneNumbers/{0}";


            [SetUp]
            public void ValidateNumberAsyncTests_SetUp()
            {
                eventLogService.ClearReceivedCalls();
                twilioRestClient.ClearReceivedCalls();

                twilioRestClient.RequestAsync(Arg.Any<Request>()).ReturnsForAnyArgs(args =>
                    Task.FromResult(new Response(HttpStatusCode.OK, $"{{ valid: true }}")));

                TwilioClient.SetRestClient(twilioRestClient);
                TwilioSmsModule.TwilioClientInitialized = true;

                twilioSmsClient = new TwilioSmsClient(eventLogService, localizationService, settingsService);
            }


            [Test]
            public async Task ValidateNumberAsync_DuplicateRequest_UsesCache()
            {
                var phoneNumber = "1112223333";
                var expectedPath = String.Format(VALIDATION_PATH, phoneNumber);
                await twilioSmsClient.ValidatePhoneNumberAsync(phoneNumber);
                await twilioSmsClient.ValidatePhoneNumberAsync(phoneNumber);

                await twilioRestClient.Received(1).RequestAsync(Arg.Is<Request>(arg => arg.Uri.AbsolutePath.EndsWith(expectedPath, StringComparison.OrdinalIgnoreCase)));
            }


            [Test]
            public async Task ValidateNumberAsync_EmptyNumber_ReturnsError()
            {
                var expectedError = "empty number";
                localizationService.GetString("Kentico.Xperience.Twilio.SMS.Error.EmptyValidationNumber").Returns(expectedError);
                var result = await twilioSmsClient.ValidatePhoneNumberAsync(String.Empty);

                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorMessage, Is.EqualTo(expectedError));
                await twilioRestClient.DidNotReceiveWithAnyArgs().RequestAsync(Arg.Any<Request>());
                eventLogService.Received().LogEvent(Arg.Is<EventLogData>(arg => arg.EventDescription.Equals(expectedError, StringComparison.OrdinalIgnoreCase)));
            }


            [Test]
            public async Task ValidateNumberAsync_ValidParameters_SendsRequest()
            {
                var countryCode = "CZ";
                var phoneNumber = "1112223333";
                var expectedPath = String.Format(VALIDATION_PATH, phoneNumber);
                var result = await twilioSmsClient.ValidatePhoneNumberAsync(phoneNumber, countryCode);

                Assert.That(result.Success, Is.True);
                Assert.That(result.Valid, Is.True);
                Assert.That(result.ErrorMessage, Is.Null);
                await twilioRestClient.Received().RequestAsync(Arg.Is<Request>(arg =>
                    arg.Uri.AbsolutePath.EndsWith(expectedPath, StringComparison.OrdinalIgnoreCase)
                 && arg.QueryParams.SingleOrDefault(kvp => kvp.Key.Equals("CountryCode", StringComparison.OrdinalIgnoreCase)).Value.Equals(countryCode, StringComparison.OrdinalIgnoreCase)));
            }
        }
    }
}
