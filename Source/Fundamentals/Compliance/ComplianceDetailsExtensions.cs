// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Compliance
{
    /// <summary>
    /// Extension methods for providing compliance details.
    /// </summary>
    public static class ComplianceDetailsExtensions
    {
        /// <summary>
        /// Get the compliance metadata details from a <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type to get from.</param>
        /// <returns>The details - empty string if none is set.</returns>
        public static string GetComplianceMetadataDetails(this Type type) => type.GetCustomAttribute<ComplianceDetailsAttribute>()?.Details ?? string.Empty;

        /// <summary>
        /// Get the compliance metadata details from a <see cref="PropertyInfo"/>.
        /// </summary>
        /// <param name="property">Property to get from.</param>
        /// <returns>The details - empty string if none is set.</returns>
        public static string GetComplianceMetadataDetails(this PropertyInfo property) =>
            property.GetCustomAttribute<ComplianceDetailsAttribute>()?.Details ??
            property.DeclaringType?.GetCustomAttribute<ComplianceDetailsAttribute>()?.Details ??
            property.PropertyType.GetComplianceMetadataDetails() ??
            string.Empty;
    }
}
