// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Confidentiality;

/// <summary>
/// Defines a provider of <see cref="SecurityMetadata"/> for <see cref="PropertyInfo">properties</see>.
/// </summary>
/// <remarks>
/// This is the security counterpart to <see cref="Compliance.ICanProvideComplianceMetadataForProperty"/> - a
/// deliberately separate interface, discovered separately, so a security metadata provider never ends up in a
/// compliance metadata resolver's provider pool or vice versa.
/// </remarks>
public interface ICanProvideSecurityMetadataForProperty
{
    /// <summary>
    /// Checks whether or not it can provide for the property.
    /// </summary>
    /// <param name="property"><see cref="PropertyInfo"/> to check for.</param>
    /// <returns>True if it can provide, false if not.</returns>
    bool CanProvide(PropertyInfo property);

    /// <summary>
    /// Provide the metadata for the property.
    /// </summary>
    /// <param name="property"><see cref="PropertyInfo"/> to provide for.</param>
    /// <returns>Provided <see cref="SecurityMetadata"/>.</returns>
    SecurityMetadata Provide(PropertyInfo property);
}
