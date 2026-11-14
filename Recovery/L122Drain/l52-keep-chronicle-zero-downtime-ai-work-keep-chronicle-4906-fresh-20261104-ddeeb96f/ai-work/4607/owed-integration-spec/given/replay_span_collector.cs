// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry;

namespace Cratis.Chronicle.Integration.for_Telemetry.given;

public class replay_span_collector : BaseExporter<Activity>
{
    public ConcurrentQueue<Activity> Spans { get; } = new();

    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var span in batch) Spans.Enqueue(span);
        return ExportResult.Success;
    }
}
