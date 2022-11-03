using CMS.Core;
using CMS.FormEngine.Web.UI;
using CMS.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;

using Twilio.Rest.Messaging.V1;

namespace Kentico.Xperience.Twilio.SMS.Controls
{
    /// <summary>
    /// A form control which displays the available Messaging Services in a drop-down list.
    /// </summary>
    public partial class TwilioServiceSelector : FormEngineUserControl
    {
        private string mValue;
        private const string CACHEKEY_SERVICES = "Twilio|SMS|ServiceResource";


        /// <inheritdoc/>
        public override object Value
        {
            get
            {
                return drpSenders.SelectedValue;
            }
            set
            {
                mValue = ValidationHelper.GetString(value, String.Empty);
            }
        }


        /// <inheritdoc/>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var services = GetMessagingServices();
            if (services.Any())
            {
                foreach (var service in services)
                {
                    drpSenders.Items.Add(new ListItem(service.FriendlyName, service.Sid));
                }

                if (!String.IsNullOrEmpty(mValue) && services.Any(s => s.Sid.Equals(mValue, StringComparison.OrdinalIgnoreCase)))
                {
                    drpSenders.SelectedValue = mValue;
                }
            }
            else
            {
                drpSenders.Enabled = false;
                drpSenders.ToolTipResourceString = "Kentico.Xperience.Twilio.SMS.Error.ClientNotInitialized";
            }
        }


        private IEnumerable<ServiceResource> GetMessagingServices()
        {
            return CacheHelper.Cache((cs) =>
            {
                try
                {
                    if (!TwilioSMSModule.TwilioClientInitialized)
                    {
                        return Enumerable.Empty<ServiceResource>();
                    }

                    return ServiceResource.Read().AsEnumerable();
                }
                catch (Exception ex)
                {
                    cs.Cached = false;
                    Service.Resolve<IEventLogService>().LogException(nameof(TwilioServiceSelector), nameof(GetMessagingServices), ex);

                    return Enumerable.Empty<ServiceResource>();
                }
            }, new CacheSettings(30, CACHEKEY_SERVICES));
        }
    }
}