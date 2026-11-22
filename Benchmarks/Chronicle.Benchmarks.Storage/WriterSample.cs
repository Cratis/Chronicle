// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>One writer's append timing, retained in launch order rather than latency order.</summary>
/// <param name="WriterIndex">The zero-based writer index in the wave.</param>
/// <param name="SequenceIndex">The setup-local sequence index: zero is shared, writer index plus one is dedicated.</param>
/// <param name="LaunchOffsetMs">Milliseconds from the start of the wave to this writer's stopwatch start.</param>
/// <param name="DurationMs">Milliseconds from the writer's stopwatch start to completion or failure.</param>
/// <param name="Succeeded">Whether the append succeeded.</param>
public sealed record WriterSample(int WriterIndex, int SequenceIndex, double LaunchOffsetMs, double DurationMs, bool Succeeded);
