// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.when_validating;

public class and_a_counter_exceeds_its_range : given.a_valid_commit
{
    const ulong TooLarge = (ulong)long.MaxValue + 1;

    [Theory]
    [InlineData("expected")]
    [InlineData("revision")]
    [InlineData("generation")]
    [InlineData("activation_epoch")]
    [InlineData("schedule_ticket")]
    [InlineData("ready_ticket")]
    [InlineData("lane_attempts")]
    [InlineData("u")]
    [InlineData("w")]
    [InlineData("next_batch_end")]
    [InlineData("next")]
    [InlineData("from")]
    [InlineData("to")]
    [InlineData("schedule_clock")]
    [InlineData("retry_due")]
    [InlineData("parked_attempts")]
    [InlineData("format")]
    [InlineData("key_definition_version")]
    [InlineData("map_version")]
    [InlineData("schema_version")]
    [InlineData("row_generation")]
    [InlineData("row_revision")]
    [InlineData("delete_generation")]
    [InlineData("delete_from")]
    void should_reject_each_out_of_range_field(string field)
    {
        var commit = Invalid(field);
        Catch.Exception(() => DeliveryCommitValidator.Validate(commit)).ShouldBeOfExactType<DeliveryValueOutOfRange>();
    }

    DeliveryCommit Invalid(string field) => field switch
    {
        "expected" => _commit with { Expected = new(TooLarge) },
        "revision" => Create(_root with { Revision = new(TooLarge) }, [], []),
        "generation" => Create(_root with { Generation = new(TooLarge) }, [], []),
        "activation_epoch" => Create(_root with { ActivationEpoch = new(TooLarge) }, [], []),
        "schedule_ticket" => Create(_root with { ScheduleTicket = new(TooLarge) }, [], []),
        "ready_ticket" => Create(_root, [ParkedKeyOperation.Insert(_parked with { ReadyTicket = new(TooLarge) })], []),
        "lane_attempts" => Lane(_root.Lanes[0] with { Attempts = TooLarge }),
        "u" => Lane(_root.Lanes[0] with { U = new(TooLarge) }),
        "w" => Lane(_root.Lanes[0] with { W = new(TooLarge) }),
        "next_batch_end" => Lane(_root.Lanes[0] with { NextBatchEnd = new(TooLarge) }),
        "next" => Create(_root, [ParkedKeyOperation.Insert(_parked with { Next = new(TooLarge) })], []),
        "from" => Create(_root, [], [SkipRangeOperation.Insert(_skip with { From = new(TooLarge) })]),
        "to" => Create(_root, [], [SkipRangeOperation.Insert(_skip with { To = new(TooLarge) })]),
        "schedule_clock" => Create(_root with { ScheduleClock = DeliveryRanges.MaximumClock + 1 }, [], []),
        "retry_due" => Create(_root, [ParkedKeyOperation.Insert(_parked with { RetryDue = DeliveryRanges.MaximumClock + 1 })], []),
        "parked_attempts" => Create(_root, [ParkedKeyOperation.Insert(_parked with { Attempts = -1 })], []),
        "format" => Create(_root with { Format = new(0) }, [], []),
        "key_definition_version" => Create(_root with { KeyDefinitionVersion = -1 }, [], []),
        "map_version" => Create(_root with { MapVersion = -1 }, [], []),
        "schema_version" => Create(_root with { CoverageHead = new(-1, ReadOnlySpan<byte>.Empty) }, [], []),
        "row_generation" => Create(_root, [ParkedKeyOperation.Insert(_parked with { Generation = new(TooLarge) })], []),
        "row_revision" => Create(_root, [], [SkipRangeOperation.Insert(_skip with { Revision = new(TooLarge) })]),
        "delete_generation" => Create(_root, [ParkedKeyOperation.Delete(new(TooLarge), _parked.Key.Hash)], []),
        _ => Create(_root, [], [SkipRangeOperation.Delete(_skip.Generation, _skip.Key.Hash, new(TooLarge))])
    };

    DeliveryCommit Lane(DeliveryLane lane) => Create(WithLanes(_root, [lane]), [], []);
}
