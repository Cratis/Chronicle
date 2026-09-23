// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Confidentiality;

/// <summary>
/// Defines a provider of <see cref="SecurityMetadata"/> for <see cref="Type">types</see>.
/// </summary>
/// <remarks>
/// This is the security counterpart to <see cref="Compliance.ICanProvideComplianceMetadataForType"/> - a
/// deliberately separate interface, discovered separately, so a security metadata provider never ends up in a
/// compliance metadata resolver's provider pool or vice versa.
/// </remarks>
public interface ICanProvideSecurityMetadataForType
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
    /// <returns>Provided <see cref="SecurityMetadata"/>.</returns>
    SecurityMetadata Provide(Type type);
}
