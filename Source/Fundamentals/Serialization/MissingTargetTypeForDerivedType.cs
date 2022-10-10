// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization;

/// <summary>
/// Exception that gets thrown when a derived type implements more than one interface and it is not specified which to use in the <see cref="DerivedTypeAttribute"/>.
/// </summary>
public class MissingTargetTypeForDerivedType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AmbiguousTargetTypeForDerivedType"/> class.
    /// </summary>
    /// <param name="type">Type that is missing interfaces.</param>
    public MissingTargetTypeForDerivedType(Type type) : base($"Type '{type.FullName}' does not implement any interface it is a derived type of. Remember there can be only one (the Highlander principle) it represents as a derived type. The DerivedTypeAttribute has a parameter for specifying this interface if you need multiple")
    {
    }
}
