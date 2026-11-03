// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Observation.for_ObserverMetrics.given.Otlp;

/// <summary>
/// Test-only decoding subset, pinned to OTLP v1.3.2.
/// </summary>
/// <seealso href="https://github.com/open-telemetry/opentelemetry-proto/blob/40b3c1b746767cbc13c2e39da3eaf1a23e54ffdd/opentelemetry/proto/metrics/v1/metrics.proto"/>
[ProtoContract]
public class OtlpMetric
{
    [ProtoMember(1)]
    public string Name { get; set; } = string.Empty;

    [ProtoMember(2)]
    public string Description { get; set; } = string.Empty;

    [ProtoMember(3)]
    public string Unit { get; set; } = string.Empty;

    [ProtoMember(7)]
    public OtlpSum? Sum { get; set; }
}
