// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization;

/// <summary>
/// Exception that gets thrown when a derived type implements more than one interface and it is not specified which to use in the <see cref="DerivedTypeAttribute"/>.
/// </summary>
public class AmbiguousTargetTypeForDerivedType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AmbiguousTargetTypeForDerivedType"/> class.
    /// </summary>
    /// <param name="type">Type that has multiple interfaces implemented.</param>
    public AmbiguousTargetTypeForDerivedType(Type type) : base($"Type '{type.FullName}' implements interfaces without specifying which interface it is a derived type of, there can be only one (the Highlander principle). The DerivedTypeAttribute has a parameter for specifying the interface.")
    {
    }
}
