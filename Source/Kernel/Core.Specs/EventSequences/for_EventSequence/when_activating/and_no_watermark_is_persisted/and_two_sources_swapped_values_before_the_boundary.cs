// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_two_sources_swapped_values_before_the_boundary : given.a_catching_up_sequence
{
    AppendResult _refused;
    bool _expire = true;

    async Task Establish()
    {
        await Seed(0, "first", email: "b");
        await Seed(1, "second", email: "a");
        await Hold("first", "a");
        await Hold("second", "b");
        _unique.Remove(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await _claims.Remove(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<string>(2));
                if (_expire) _clock.Advance(TimeSpan.FromSeconds(2));
                _expire = false;
            });
    }

    async Task Because()
    {
        await Tick();
        _refused = await AppendAnEvent();
        await Tick();
    }

    [Fact] void should_refuse_while_releases_and_claims_span_turns() => _refused.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] async Task should_leave_the_first_source_holding_its_final_value() => (await Values("first")).ShouldContainOnly(Hash("b"));
    [Fact] async Task should_leave_the_second_source_holding_its_final_value() => (await Values("second")).ShouldContainOnly(Hash("a"));
    [Fact] void should_clear_the_marker_after_the_last_write() => _snapshots.Written!.ConstraintIndexCatchUpRefusing.ShouldBeNull();
}
