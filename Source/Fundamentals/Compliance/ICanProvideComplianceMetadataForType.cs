// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance
{
    /// <summary>
    /// Defines a provider of <see cref="ComplianceMetadata"/> for <see cref="Type">types</see>.
    /// </summary>
    public interface ICanProvideComplianceMetadataForType
    {
        /// <summary>
        /// Checks whether or not it can provide for the type.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to check for.</param>
        /// <returns>True if it can provide, false if not.</returns>
        bool CanProvide(Type type);

        /// <summary>
        /// Provide the metadata for the type.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to provide for.</param>
        /// <returns>Provided <see cref="ComplianceMetadata"/>.</returns>
        ComplianceMetadata Provide(Type type);
    }
}
