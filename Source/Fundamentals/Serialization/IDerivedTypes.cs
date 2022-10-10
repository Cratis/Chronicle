// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization;

/// <summary>
/// Defines a system that tracks derived types and the target type they represents.
/// </summary>
public interface IDerivedTypes
{
    /// <summary>
    /// Get the derived type for a target type.
    /// </summary>
    /// <param name="targetType">Target type to get for.</param>
    /// <param name="derivedTypeId">The unique identifier of the derived type.</param>
    /// <returns>The derived type.</returns>
    Type GetDerivedTypeFor(Type targetType, DerivedTypeId derivedTypeId);

    /// <summary>
    /// Get the target type from a derived type.
    /// </summary>
    /// <param name="derivedType">Derived type to get from.</param>
    /// <returns>The target type.</returns>
    Type GetTargetTypeFor(Type derivedType);
}
