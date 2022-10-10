// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Serialization;

/// <summary>
/// Attribute used to adorn types that represent a unique type in the system and serializers need to recognize.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DerivedTypeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DerivedTypeAttribute"/> class.
    /// </summary>
    /// <param name="identifier">String representation of a <see cref="Guid"/>.</param>
    /// <param name="targetType">Optional target type the derived type is for.</param>
    public DerivedTypeAttribute(string identifier, Type? targetType = default)
    {
        Identifier = identifier;
        TargetType = targetType;
    }

    /// <summary>
    /// Gets the unique identifier of the derived type.
    /// </summary>
    public DerivedTypeId Identifier { get; }

    /// <summary>
    /// Gets the optional target type.
    /// </summary>
    public Type? TargetType { get; }
}
