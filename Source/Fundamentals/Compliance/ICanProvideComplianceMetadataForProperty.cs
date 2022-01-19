// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Compliance
{
    /// <summary>
    /// Defines a provider of <see cref="ComplianceMetadata"/> for <see cref="PropertyInfo">types</see>.
    /// </summary>
    public interface ICanProvideComplianceMetadataForProperty
    {
        /// <summary>
        /// Checks whether or not it can provide for the type.
        /// </summary>
        /// <param name="property"><see cref="Type"/> to check for.</param>
        /// <returns>True if it can provide, false if not.</returns>
        bool CanProvide(PropertyInfo property);

        /// <summary>
        /// Provide the metadata for the type.
        /// </summary>
        /// <param name="property"><see cref="Type"/> to provide for.</param>
        /// <returns>Provided <see cref="ComplianceMetadata"/>.</returns>
        ComplianceMetadata Provide(PropertyInfo property);
    }
}
