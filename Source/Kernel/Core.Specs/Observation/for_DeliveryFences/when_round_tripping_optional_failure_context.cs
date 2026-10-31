// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_DeliveryFences;

public class when_round_tripping_optional_failure_context : Specification
{
    FailedPartitionAttempt _result;
    readonly DeliveryFenceContext _context = new(DateTimeOffset.UnixEpoch.AddSeconds(330), [new("type", "key", "A", "silo")]);

    void Because()
    {
        var attempt = new FailedPartitionAttempt { Kind = FailureKind.Timeout, DeliveryFence = _context };
        _result = JsonSerializer.Deserialize<FailedPartitionAttempt>(JsonSerializer.Serialize(attempt))!;
    }

    [Fact] void should_preserve_the_original_deadline() => _result.DeliveryFence!.ReleaseBy.ShouldEqual(_context.ReleaseBy);
    [Fact] void should_preserve_the_original_identity() => _result.DeliveryFence!.Targets.ShouldContainOnly(_context.Targets);
    [Fact] void should_preserve_the_timeout_kind() => _result.Kind.ShouldEqual(FailureKind.Timeout);
    [Fact] void should_read_legacy_attempts_without_context() => JsonSerializer.Deserialize<FailedPartitionAttempt>("{}")!.DeliveryFence.ShouldBeNull();
}
