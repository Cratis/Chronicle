// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Chronicle.Benchmarks;

[EventType("c0b93c8e-3f3f-4f3f-8f3f-3f3f3f3f3f3f")]
public record BenchmarkEvent(string Name, int Value, DateTimeOffset Timestamp);
