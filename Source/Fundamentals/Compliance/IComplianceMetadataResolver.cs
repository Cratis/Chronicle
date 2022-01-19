// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Compliance
{
    /// <summary>
    /// Defines a resolver of <see cref="ComplianceMetadata"/> for types and properties.
    /// </summary>
    public interface IComplianceMetadataResolver
    {
        /// <summary>
        /// Check whether or not a specific <see cref="Type"/> has any <see cref="ComplianceMetadata"/> associated with it.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to check.</param>
        /// <returns>True if it has, false if not.</returns>
        bool HasMetadataFor(Type type);

        /// <summary>
        /// Check whether or not a specific <see cref="PropertyInfo"/> has any <see cref="ComplianceMetadata"/> associated with it.
        /// </summary>
        /// <param name="property"><see cref="PropertyInfo"/> to check.</param>
        /// <returns>True if it has, false if not.</returns>
        bool HasMetadataFor(PropertyInfo property);

        /// <summary>
        /// Get the <see cref="ComplianceMetadata"/> associated with a <see cref="Type"/>.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to get for.</param>
        /// <returns>Collection of <see cref="ComplianceMetadata"/> associated with the type.</returns>
        IEnumerable<ComplianceMetadata> GetMetadataFor(Type type);

        /// <summary>
        /// Get the <see cref="ComplianceMetadata"/> associated with a <see cref="PropertyInfo"/>.
        /// </summary>
        /// <param name="property"><see cref="PropertyInfo"/> to get for.</param>
        /// <returns>Collection of <see cref="ComplianceMetadata"/> associated with the type.</returns>
        IEnumerable<ComplianceMetadata> GetMetadataFor(PropertyInfo property);
    }
}
