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
                settingsService[SETTINGSKEY].Returns(MESSAGING_SERVICE);

                twilioRestClient.ClearReceivedCalls();
                twilioRestClient.AccountSid.Returns(ACCOUNT);
                twilioRestClient.RequestAsync(Arg.Any<Request>()).ReturnsForAnyArgs(args =>
                    Task.FromResult(new Response(HttpStatusCode.OK, $"{{ status:'{MessageResource.StatusEnum.Queued}' }}")));

                TwilioClient.SetRestClient(twilioRestClient);
                TwilioSmsModule.TwilioClientInitialized = true;

                twilioSmsClient = new TwilioSmsClient(settingsService);
            }


            [Test]
            public async Task SendMessageAsync_EmptyBody_ThrowsException()
            {
                var options = new CreateMessageOptions("+12223334444");

                Assert.ThrowsAsync<InvalidOperationException>(async() => await twilioSmsClient.SendMessageAsync(options),
                    "Message body cannot be empty.");
                await twilioRestClient.DidNotReceiveWithAnyArgs().RequestAsync(Arg.Any<Request>());
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
                var response = await twilioSmsClient.SendMessageAsync(options);

                Assert.Multiple(() =>
                {
                    Assert.That(response, Is.Not.Null);
                    Assert.That(response.Status == MessageResource.StatusEnum.Queued);
                });
                await twilioRestClient.Received().RequestAsync(Arg.Is<Request>(arg =>
                    arg.Uri.AbsolutePath.EndsWith(MESSAGING_PATH, StringComparison.OrdinalIgnoreCase)
                 && arg.PostParams.SingleOrDefault(kvp => kvp.Key.Equals("To", StringComparison.OrdinalIgnoreCase)).Value.Equals(expectedRecipient, StringComparison.OrdinalIgnoreCase)
                 && arg.PostParams.SingleOrDefault(kvp => kvp.Key.Equals("Body", StringComparison.OrdinalIgnoreCase)).Value.Equals(SMS_BODY, StringComparison.OrdinalIgnoreCase)
                 && arg.PostParams.SingleOrDefault(kvp => kvp.Key.Equals("From", StringComparison.OrdinalIgnoreCase)).Value.Equals(expectedSender, StringComparison.OrdinalIgnoreCase)));
            }


            [Test]
            public async Task SendMessageAsync_InvalidNumber_ThrowsException()
            {
                var recipient = "1112223333";
                var options = new CreateMessageOptions(recipient)
                {
                    Body = SMS_BODY
                };

                Assert.ThrowsAsync<InvalidOperationException>(async() => await twilioSmsClient.SendMessageAsync(options),
                    $"The number '{recipient}' is not in a valid Twilio format.");
                await twilioRestClient.DidNotReceiveWithAnyArgs().RequestAsync(Arg.Any<Request>());
            }


            [Test]
            public async Task SendMessageAsync_ValidOptions_SendsMessage()
            {
                var expectedRecipient = "+12223334444";
                var options = new CreateMessageOptions(expectedRecipient)
                {
                    Body = SMS_BODY
                };
                var response = await twilioSmsClient.SendMessageAsync(options);

                Assert.Multiple(() =>
                {
                    Assert.That(response, Is.Not.Null);
                    Assert.That(response.Status == MessageResource.StatusEnum.Queued);
                });
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
                twilioRestClient.ClearReceivedCalls();
                twilioRestClient.RequestAsync(Arg.Any<Request>()).ReturnsForAnyArgs(args =>
                    Task.FromResult(new Response(HttpStatusCode.OK, "{ valid:true }")));

                TwilioClient.SetRestClient(twilioRestClient);
                TwilioSmsModule.TwilioClientInitialized = true;

                twilioSmsClient = new TwilioSmsClient(settingsService);
            }


            [Test]
            public async Task ValidateNumberAsync_DuplicateRequest_UsesCache()
            {
                var phoneNumber = "1112223333";
                var expectedPath = String.Format(VALIDATION_PATH, phoneNumber);
                await twilioSmsClient.ValidatePhoneNumberAsync(phoneNumber);
                var response = await twilioSmsClient.ValidatePhoneNumberAsync(phoneNumber);

                Assert.Multiple(() =>
                {
                    Assert.That(response, Is.Not.Null);
                    Assert.That(response.Valid, Is.True);
                });
                await twilioRestClient.Received(1).RequestAsync(Arg.Is<Request>(arg => arg.Uri.AbsolutePath.EndsWith(expectedPath, StringComparison.OrdinalIgnoreCase)));
            }


            [Test]
            public async Task ValidateNumberAsync_EmptyNumber_ThrowsException()
            {
                Assert.ThrowsAsync<ArgumentNullException>(async() => await twilioSmsClient.ValidatePhoneNumberAsync(String.Empty));
                await twilioRestClient.DidNotReceiveWithAnyArgs().RequestAsync(Arg.Any<Request>());
            }


            [Test]
            public void ValidateNumberAsync_InvalidCountryCode_ThrowsException()
            {
                var countryCode = "en-US";

                Assert.ThrowsAsync<InvalidOperationException>(async() => await twilioSmsClient.ValidatePhoneNumberAsync("+12223334444", countryCode),
                    $"The country code '{countryCode}' isn't a valid 2-letter country code.");
            }


            [Test]
            public async Task ValidateNumberAsync_ValidParameters_SendsRequest()
            {
                var countryCode = "CZ";
                var phoneNumber = "1112223333";
                var expectedPath = String.Format(VALIDATION_PATH, phoneNumber);
                var response = await twilioSmsClient.ValidatePhoneNumberAsync(phoneNumber, countryCode);

                Assert.Multiple(() =>
                {
                    Assert.That(response, Is.Not.Null);
                    Assert.That(response.Valid, Is.True);
                });
                await twilioRestClient.Received().RequestAsync(Arg.Is<Request>(arg =>
                    arg.Uri.AbsolutePath.EndsWith(expectedPath, StringComparison.OrdinalIgnoreCase)
                 && arg.QueryParams.SingleOrDefault(kvp => kvp.Key.Equals("CountryCode", StringComparison.OrdinalIgnoreCase)).Value.Equals(countryCode, StringComparison.OrdinalIgnoreCase)));
            }
        }
    }
}
