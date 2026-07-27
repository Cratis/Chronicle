// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Event appended by the append benchmarks. Deliberately has no observer, so the measured window covers
/// the append path alone.
/// </summary>
/// <param name="Name">The event name.</param>
/// <param name="Value">The benchmark value.</param>
[EventType]
public record AppendOnlyEvent(string Name, int Value);
