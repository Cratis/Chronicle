// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_values_sit_at_their_maximum : given.a_valid_commit
{
    Exception _exception;

    void Establish()
    {
        var envelope = new VersionedEnvelope(int.MaxValue, new byte[65536].AsSpan());
        var root = _root with
        {
            Format = new(int.MaxValue),
            Lifecycle = (DeliveryLifecycle)int.MaxValue,
            Generation = new(long.MaxValue),
            KeyDefinitionVersion = int.MaxValue,
            MapVersion = int.MaxValue,
            ActivationEpoch = new(long.MaxValue),
            ScheduleClock = DeliveryRanges.MaximumClock,
            ScheduleTicket = new(long.MaxValue),
            CoverageHead = envelope,
            Operation = envelope,
            Revision = new(long.MaxValue)
        };
        root = WithLanes(root, [.. Enumerable.Repeat(new DeliveryLane(new(long.MaxValue), new(long.MaxValue), new(long.MaxValue), long.MaxValue), 64)]);
        var key = SequencingKeyIdentity.For(new ValueSequencingKey([new string('a', 8187)]));
        var parked = _parked with
        {
            Generation = root.Generation,
            Key = key,
            Next = new(long.MaxValue),
            Kind = (ParkedKeyKind)int.MaxValue,
            Attempts = int.MaxValue,
            RetryDue = DeliveryRanges.MaximumClock,
            ReadyTicket = new(long.MaxValue),
            Revision = root.Revision
        };
        var skip = _skip with { Generation = root.Generation, Key = key, From = new(long.MaxValue - 1), To = new(long.MaxValue), Reason = (SkipReason)int.MinValue, Revision = root.Revision };
        _commit = new(new(long.MaxValue - 1), root, [ParkedKeyOperation.Insert(parked)], [SkipRangeOperation.Insert(skip)]);
    }

    void Because() => _exception = Catch.Exception(() => DeliveryCommitValidator.Validate(_commit));

    [Fact] void should_accept_all_inclusive_maxima_and_preserve_unknown_enums() => _exception.ShouldBeNull();
    [Fact] void should_use_a_key_at_the_byte_limit() => _commit.ParkedKeys[0].Row.Key.Key.Span.Length.ShouldEqual(8192);
}
