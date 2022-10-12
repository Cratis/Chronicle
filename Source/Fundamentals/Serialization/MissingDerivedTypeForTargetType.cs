// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization;

/// <summary>
/// Exception that gets thrown when a derived type is missing for a target type.
/// </summary>
public class MissingDerivedTypeForTargetType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingDerivedTypeForTargetType"/> class.
    /// </summary>
    /// <param name="targetType">Type of target type.</param>
    /// <param name="derivedTypeId">The unique identifier of the expected derived type.</param>
    public MissingDerivedTypeForTargetType(Type targetType, DerivedTypeId derivedTypeId) : base($"Missing derived type with identifier '{derivedTypeId}' for target type '{targetType}'.")
    {
    }
}
