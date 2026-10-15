// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enforcing_read_protection.given;

public class an_enforcing_unit : for_UnitOfWork.given.a_unit_of_work
{
    protected ReadProtectionFrame _frame;
    protected DecisionReadCommitOwner _owner;
    protected UnitOfWork _other;
    protected IDecisionRead _read;
    protected ContributedRead _contribution;
    protected EventType _changed;
    protected Exception _error;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"tenant");
        var types = Substitute.For<IEventTypes>();
        _eventStore.EventTypes.Returns(types);
        _changed = new("stock-changed", EventTypeGeneration.First);
        types.GetEventTypeFor(typeof(object)).Returns(_changed);
        _read = new DecisionRead<object>("source", null, "store", "tenant", 100, [_changed]);
        _contribution = new(ConcurrencyScopeOrigin.Explicit, "source", new(100, "source", EventTypes: [_changed]), "store", "tenant", EventSequenceId.Log, "reservation");
        _frame = _unitOfWork.EnforceReadProtection();
        _owner = _unitOfWork.ClaimDecisionReadCommitOwnership();
        _other = new(CorrelationId.New(), _ => { }, _eventStore);
    }

    protected void Stage(EventSourceId? source = null, EventStreamType? stream = null) => _unitOfWork.AddEvent(EventSequenceId.Log, source ?? "source", new object(), Causation.Unknown(), eventStreamType: stream);

    void Destroy() => _frame.Dispose();
}
