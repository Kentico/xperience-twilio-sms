using System.Collections.Generic;

using Twilio.Rest.Lookups.V2;

namespace Kentico.Xperience.Twilio.SMS.Models
{
    /// <summary>
    /// Represents the response of a phone number validation request.
    /// </summary>
    public sealed class NumberValidationResponse
    {
        /// <summary>
        /// The phone number's country, e.g. "US."
        /// </summary>
        public string CountryCode
        {
            get;
            set;
        }


        /// <summary>
        /// The error that occurred during sending, or null if successful.
        /// </summary>
        public string ErrorMessage
        {
            get;
            set;
        }


        /// <summary>
        /// The phone number formatted for Twilio usage.
        /// </summary>
        public string FormattedNumber
        {
            get;
            set;
        }


        /// <summary>
        /// The phone number in a national format.
        /// </summary>
        public string NationalFormat
        {
            get;
            set;
        }


        /// <summary>
        /// <c>True</c> if the request was received and processed correctly.
        /// </summary>
        public bool Success
        {
            get;
            private set;
        }


        /// <summary>
        /// <c>True</c> if the phone number is valid.
        /// </summary>
        public bool? Valid
        {
            get;
            set;
        }


        /// <summary>
        /// The validation errors found when <see cref="Valid"/> is <c>false</c>.
        /// </summary>
        public IEnumerable<PhoneNumberResource.ValidationErrorEnum> ValidationErrors
        {
            get;
            set;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="NumberValidationResponse"/> class.
        /// </summary>
        /// <param name="success"><c>True</c> if the request was received and processed correctly.</param>
        public NumberValidationResponse(bool success)
        {
            Success = success;
        }
    }
}
