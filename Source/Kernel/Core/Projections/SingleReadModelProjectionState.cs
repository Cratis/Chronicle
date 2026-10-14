// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the state carried between batches of a strongly consistent single-instance projection fold.
/// </summary>
/// <param name="Model">The projected properties.</param>
/// <param name="Exists">Whether the instance exists, independently of its properties.</param>
/// <param name="IsInitialized">Whether a root event has initialized the instance.</param>
[GenerateSerializer]
public record SingleReadModelProjectionState(
    [property: Id(0)] ExpandoObject Model,
    [property: Id(1)] bool Exists,
    [property: Id(2)] bool IsInitialized)
{
    /// <summary>
    /// Gets a fresh state for an instance that has not been created.
    /// </summary>
    public static SingleReadModelProjectionState Empty => new(new ExpandoObject(), false, false);
}
