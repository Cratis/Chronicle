// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.given;

public class a_unit_with_a_pending_implicit_append : a_unit_recording_provenance
{
    protected TaskCompletionSource<AppendManyResult> _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected Exception _lateError;
    internal ScopeProvenance _captured;
    protected virtual UnitOfWorkLifecyclePolicy Policy => UnitOfWorkLifecyclePolicy.Compatibility;

    void Establish()
    {
        _unitOfWork = new(_correlationId, OnUnitOfWorkCompleted, _eventStore, Policy);
        _unitOfWork.AddImplicitScope(EventSequenceId.Log, "source", 7);
        _unitOfWork.AddEvent(EventSequenceId.Log, "source", "event", Causation.Unknown());
        ((IEventSequenceWithScopeProvenance)_eventSequence).AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<IEnumerable<NamedTag>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>(), Arg.Any<ScopeProvenance>()).Returns(call =>
        {
            _captured = call.Arg<ScopeProvenance>();
            return _pending.Task;
        });
    }

    protected async Task AttemptLateEnrollment()
    {
        var commit = _unitOfWork.Commit();
        _lateError = Catch.Exception(() => _unitOfWork.AddImplicitScope(EventSequenceId.Log, "source", 1));
        _pending.SetResult(_appendResult);
        await commit;
    }
}
