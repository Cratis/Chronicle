// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.NormalClient;

/// <summary>The classic reducer's model.</summary>
/// <typeparam name="TEvent">The event.</typeparam>
public record ClassicModel<TEvent>
{
    /// <summary>Gets the folded event count.</summary>
    public int Count { get; init; }
}
