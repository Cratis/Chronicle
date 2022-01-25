// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance.GDPR
{
    /// <summary>
    /// Represents an attribute that can be used to mark classes or properties to indicate the information kept is PII according to the definition of GDPR.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class PIIAttribute : Attribute
    {
        /// <summary>
        /// Gets the details as to why or to what purpose/extent the type or property marked is classified as PII.
        /// </summary>
        public string Details { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PIIAttribute"/> class.
        /// </summary>
        /// <param name="details">Optional details - default value is empty string.</param>
        public PIIAttribute(string details = "")
        {
            Details = details;
        }
    }
}
