// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents a count of instances to retrieve or watch.
/// </summary>
/// <param name="Value">The actual count.</param>
public record InstanceCount(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// The value representing an unlimited count.
    /// </summary>
    public static readonly InstanceCount Unlimited = int.MaxValue;

    /// <summary>
    /// The default page size for queries.
    /// </summary>
    public static readonly InstanceCount Default = 50;

    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="InstanceCount"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator InstanceCount(int value) => new(value);
}
