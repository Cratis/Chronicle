// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents a count of instances to skip when retrieving or watching read model instances.
/// </summary>
/// <param name="Value">The actual count.</param>
public record InstanceCountToSkip(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// The value representing no instances to skip.
    /// </summary>
    public static readonly InstanceCountToSkip Zero = 0;

    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="InstanceCountToSkip"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator InstanceCountToSkip(int value) => new(value);
}
