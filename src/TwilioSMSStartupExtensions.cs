﻿using Kentico.Xperience.Twilio.SMS.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System;

using Twilio;

namespace Kentico.Xperience.Twilio.SMS
{
    /// <summary>
    /// Application startup extension methods.
    /// </summary>
    public static class TwilioSMSStartupExtensions
    {
        /// <summary>
        /// Initializes the <see cref="TwilioClient"/> for use by the Kentico Xperience Twilio SMS integration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        public static IServiceCollection AddTwilioSMS(this IServiceCollection services, IConfiguration configuration)
        {
            var options = new TwilioSMSOptions();
            configuration.GetSection(TwilioSMSOptions.SECTION_NAME).Bind(options);
            if (!String.IsNullOrEmpty(options.AuthToken) && !String.IsNullOrEmpty(options.AccountSid))
            {
                TwilioClient.Init(options.AccountSid, options.AuthToken);
                TwilioSMSModule.TwilioClientInitialized = true;
            }

            return services;
        }
    }
}
