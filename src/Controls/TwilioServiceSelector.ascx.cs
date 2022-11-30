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

            if (!IsPostBack)
            {
                InitDropDown();
            }
        }


        private void InitDropDown()
        {
            drpSenders.Items.Add(new ListItem("(none)", String.Empty));

            var services = GetMessagingServices();
            if (!services.Any())
            {
                drpSenders.Enabled = false;
                drpSenders.ToolTipResourceString = "Kentico.Xperience.Twilio.SMS.Error.ClientNotInitialized";
                return;
            }

            foreach (var service in services)
            {
                drpSenders.Items.Add(new ListItem(service.FriendlyName, service.Sid));
                if (!String.IsNullOrEmpty(mValue) && service.Sid.Equals(mValue, StringComparison.OrdinalIgnoreCase))
                {
                    drpSenders.SelectedValue = mValue;
                }
            }
        }


        private IEnumerable<ServiceResource> GetMessagingServices()
        {
            if (!TwilioSmsModule.TwilioClientInitialized)
            {
                return Enumerable.Empty<ServiceResource>();
            }

            try
            {
                return ServiceResource.Read().AsEnumerable();
            }
            catch (Exception ex)
            {
                Service.Resolve<IEventLogService>().LogException(nameof(TwilioServiceSelector), nameof(GetMessagingServices), ex);

                return Enumerable.Empty<ServiceResource>();
            }
        }
    }
}