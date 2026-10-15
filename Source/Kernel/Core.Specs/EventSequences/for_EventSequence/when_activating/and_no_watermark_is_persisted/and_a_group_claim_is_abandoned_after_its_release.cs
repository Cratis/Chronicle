// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_a_group_claim_is_abandoned_after_its_release : given.a_catching_up_sequence
{
    readonly TaskCompletionSource _abandoned = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _hang = true;
    AppendResult _refused;

    async Task Establish()
    {
        await TransferHistory();
        _unique.Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _claimWrites++;
                if (!_hang) return _claims.Save(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<UniqueConstraintValue>(3), call.ArgAt<string>(4));

                // This store deliberately ignores cancellation; the signal is completed in teardown.
                _clock.Advance(TimeSpan.FromSeconds(1));
                return _abandoned.Task;
            });
    }

    async Task Because()
    {
        await Tick();
        if (_claimWrites == 0) return;
        _refused = await AppendAnEvent();
        _hang = false;
        _clock.Advance(TimeSpan.FromSeconds(1));
        await Tick();
        await Tick();
        await Tick();
    }

    void Destroy() => _abandoned.TrySetResult();

    [Fact] void should_refuse_after_the_unconfirmed_claim() => _refused.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_attempt_the_claim_once_per_turn() => _claimWrites.ShouldEqual(2);
    [Fact] async Task should_restore_the_claim_on_the_retry_turn() => (await Values("new-holder")).ShouldContainOnly(Hash("value"));
}
