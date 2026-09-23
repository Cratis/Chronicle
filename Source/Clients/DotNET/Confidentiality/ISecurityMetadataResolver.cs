// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Confidentiality;

/// <summary>
/// Defines a resolver of <see cref="SecurityMetadata"/> for types and properties.
/// </summary>
/// <remarks>
/// This is the security counterpart to <see cref="Compliance.IComplianceMetadataResolver"/> - a deliberately
/// separate resolver over a deliberately separate provider pool, so a schema's security classification can never
/// be a side effect of its compliance providers, or the other way around.
/// </remarks>
public interface ISecurityMetadataResolver
{
    /// <summary>
    /// Check whether or not a specific <see cref="Type"/> has any <see cref="SecurityMetadata"/> associated with it.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if it has, false if not.</returns>
    bool HasMetadataFor(Type type);

    /// <summary>
    /// Check whether or not a specific <see cref="PropertyInfo"/> has any <see cref="SecurityMetadata"/> associated with it.
    /// </summary>
    /// <param name="property"><see cref="PropertyInfo"/> to check.</param>
    /// <returns>True if it has, false if not.</returns>
    bool HasMetadataFor(PropertyInfo property);

    /// <summary>
    /// Get the <see cref="SecurityMetadata"/> associated with a <see cref="Type"/>.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get for.</param>
    /// <returns>Collection of <see cref="SecurityMetadata"/> associated with the type.</returns>
    IEnumerable<SecurityMetadata> GetMetadataFor(Type type);

    /// <summary>
    /// Get the <see cref="SecurityMetadata"/> associated with a <see cref="PropertyInfo"/>.
    /// </summary>
    /// <param name="property"><see cref="PropertyInfo"/> to get for.</param>
    /// <returns>Collection of <see cref="SecurityMetadata"/> associated with the type.</returns>
    IEnumerable<SecurityMetadata> GetMetadataFor(PropertyInfo property);
}
