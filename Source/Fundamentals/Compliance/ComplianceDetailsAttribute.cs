// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Compliance
{
    /// <summary>
    /// Attribute to adorn for providing the details as to why or to what purpose/extent the type or property marked is classified as PII.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter)]
    public class ComplianceDetailsAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComplianceDetailsAttribute"/> class.
        /// </summary>
        /// <param name="details">The details as to why or to what purpose/extent the type or property marked is classified as PII.</param>
        public ComplianceDetailsAttribute(string details)
        {
            Details = details;
        }

        /// <summary>
        /// Gets the details as to why or to what purpose/extent the type or property marked is classified as PII.
        /// </summary>
        public string Details { get; }
    }
}
