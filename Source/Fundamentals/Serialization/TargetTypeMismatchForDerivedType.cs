// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization;

/// <summary>
/// Exception that gets thrown when a derived type implements more than one interface and it is not specified which to use in the <see cref="DerivedTypeAttribute"/>.
/// </summary>
public class TargetTypeMismatchForDerivedType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AmbiguousTargetTypeForDerivedType"/> class.
    /// </summary>
    /// <param name="type">Type that is missing interfaces.</param>
    public TargetTypeMismatchForDerivedType(Type type) : base($"The specified target type used for '{type.FullName}' does not match any of the interfaces.")
    {
    }
}
