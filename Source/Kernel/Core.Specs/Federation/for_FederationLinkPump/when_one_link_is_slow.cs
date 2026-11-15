// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_one_link_is_slow : given.a_link_pump
{
    TaskCompletionSource<QueryResult<FederatedEventBatchResponse>> _release;
    given.FakePull _slowPull;
    given.FakeInbox _slowInbox;
    FederationLinkPump _slowPump;
    FederationBindingState _slowBinding;
    Task<TimeSpan?> _slowStep;
    TimeSpan? _fastDelay;
    bool _slowFinishedWhileFastCompleted;

    void Establish()
    {
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _slowPull = new() { Handler = _ => _release.Task };
        _slowInbox = new();
        _slowBinding = CreateBinding("slow");
        _registry.GetBindings().Returns(_ => Task.FromResult<IReadOnlyList<FederationBindingState>>([_binding, _slowBinding]));
        _slowPump = CreatePump("slow", _slowPull, new(_slowPull), _slowInbox);
        _pull.Enqueue(given.FakePull.Batch("remote", 2, 1, given.FakePull.Event(1)));
    }

    async Task Because()
    {
        _slowStep = _slowPump.Step(default);
        _fastDelay = await _pump.Step(default).WaitAsync(TimeSpan.FromSeconds(10));
        _slowFinishedWhileFastCompleted = _slowStep.IsCompleted;
        _release.SetResult(QueryResult<FederatedEventBatchResponse>.Success(Guid.NewGuid(), given.FakePull.Batch("remote", 1, 0)));
        await _slowStep;
    }

    void Destroy() => _slowPump.Dispose();

    [Fact] void should_not_have_finished_the_slow_link_yet() => _slowFinishedWhileFastCompleted.ShouldBeFalse();
    [Fact] void should_let_the_other_link_complete() => _fastDelay.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_forward_events_for_the_other_link() => _inbox.Appended.Count.ShouldEqual(1);
    [Fact] void should_keep_the_status_of_each_link_separate() => _status.GetBinding("slow").State.ShouldEqual(FederationBindingStates.Streaming);
}
