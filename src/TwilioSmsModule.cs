﻿using CMS.Base;
using CMS.DataEngine;
using CMS.Helpers;

using System;
using System.Configuration;

using Twilio;

namespace Kentico.Xperience.Twilio.SMS
{
    /// <summary>
    /// Initializes the <see cref="TwilioClient"/> used by the Twilio SMS integration.
    /// </summary>
    public sealed class TwilioSmsModule : Module
    {
        private const string APPSETTING_TWILIO_ACCOUNTSID = "TwilioAccountSID";
        private const string APPSETTING_TWILIO_AUTHTOKEN = "TwilioAuthToken";


        /// <summary>
        /// If <c>true</c>, the <see cref="TwilioClient"/> was successfully initialized on application startup.
        /// </summary>
        public static bool TwilioClientInitialized
        {
            get;
            internal set;
        }


        /// <inheritdoc/>
        public TwilioSmsModule() : base(nameof(TwilioSmsModule))
        {
        }


        /// <inheritdoc/>
        protected override void OnInit()
        {
            base.OnInit();

            if (SystemContext.IsCMSRunningAsMainApplication)
            {
                // Initialize TwilioClient for CMS application
                var accountSid = ValidationHelper.GetString(ConfigurationManager.AppSettings[APPSETTING_TWILIO_ACCOUNTSID], String.Empty);
                var authToken = ValidationHelper.GetString(ConfigurationManager.AppSettings[APPSETTING_TWILIO_AUTHTOKEN], String.Empty);
                if (!String.IsNullOrEmpty(accountSid) && !String.IsNullOrEmpty(authToken))
                {
                    TwilioClient.Init(accountSid, authToken);
                    TwilioClientInitialized = true;
                }
            }
        }
    }
}
