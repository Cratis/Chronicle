// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_a_protected_named_tagged_event : given.a_unit_of_work
{
    EventForEventSourceId[] _appended;
    IDictionary<EventSourceId, ConcurrencyScope> _scopes;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _eventSequence.AppendManyWithNamedTags(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<IEnumerable<NamedTag>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Returns(call =>
            {
                _appended = call.Arg<IEnumerable<EventForEventSourceId>>().ToArray();
                _scopes = call.Arg<IDictionary<EventSourceId, ConcurrencyScope>>();
                return _appendResult;
            });
        _unitOfWork.AddDecisionRead(new DecisionRead<object>(
            "source", null, _eventStore.Name, _eventStore.Namespace, 4, [new EventType("created", EventTypeGeneration.First)]));
        _unitOfWork.AddEventWithNamedTags(EventSequenceId.Log, "source", "created", [new("category", "protected")], Causation.Unknown());
    }

    async Task Because()
    {
        var owner = _unitOfWork.ClaimDecisionReadCommitOwnership();
        await _unitOfWork.CommitAsOwner(owner);
    }

    [Fact] void should_use_the_named_tag_path() => _appended.Single().NamedTags.Single().Value.ShouldEqual("protected");
    [Fact] void should_preserve_the_decision_scope() => _scopes.ContainsKey("source").ShouldBeTrue();
    [Fact]
    void should_not_use_the_legacy_path() => _eventSequence.DidNotReceive().AppendMany(
        Arg.Any<IEnumerable<EventForEventSourceId>>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
}
