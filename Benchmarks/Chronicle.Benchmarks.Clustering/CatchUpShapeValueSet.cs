// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Sets a deterministic value on one synthetic event source.
/// </summary>
/// <param name="Value">The value to project or handle.</param>
[EventType]
public record CatchUpShapeValueSet(int Value);
