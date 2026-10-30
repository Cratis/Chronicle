// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Event appended by <see cref="LargeBatchAppendBenchmark"/> to measure the cost a unique constraint adds to
/// an append. Like <see cref="AppendOnlyEvent"/> it has no observer.
/// </summary>
/// <param name="Name">The name, unique across every event source.</param>
/// <param name="Value">The benchmark value.</param>
[EventType]
public record UniquelyNamedEvent([property: Unique] string Name, int Value);
