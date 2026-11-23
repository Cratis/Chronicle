// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_namespace_bound_sequence : appending_many_events
{
    protected static readonly NamespaceIncarnationId Incarnation = new(Guid.NewGuid());
    protected readonly List<NamespaceCommitContext> _contexts = [];
    protected readonly List<EventToAppendToStorage> _written = [];
    protected INamespaceFencedEventSequenceStorage _fenced = null!;
    protected CorrelationId _correlationId = CorrelationId.New();

    protected override NamespaceIncarnationId? ActivationIncarnation => Incarnation;
    protected int TokenlessAppendCount => _eventSequenceStorage.ReceivedCalls().Count(call =>
        call.GetMethodInfo().DeclaringType == typeof(IEventSequenceStorage) &&
        new[] { "Append", "AppendMany", "AppendManyWithNamedTags" }.Contains(call.GetMethodInfo().Name, StringComparer.Ordinal));

    protected override IEventSequenceStorage CreateEventSequenceStorage() => Substitute.For<IEventSequenceStorage, INamespaceFencedEventSequenceStorage>();

    void Establish()
    {
        _fenced = (INamespaceFencedEventSequenceStorage)_eventSequenceStorage;
        _fenced.Append(Arg.Any<NamespaceCommitContext>(), Arg.Any<EventToAppendToStorage>()).Returns(call =>
        {
            _contexts.Add(call.Arg<NamespaceCommitContext>());
            var attempted = call.Arg<EventToAppendToStorage>();
            _written.Add(attempted);
            return Task.FromResult(Result<AppendedEvent, DuplicateEventSequenceNumber>.Success(AppendedEventsFrom([attempted]).Single()));
        });
        _fenced.AppendMany(Arg.Any<NamespaceCommitContext>(), Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(call =>
        {
            _contexts.Add(call.Arg<NamespaceCommitContext>());
            var attempted = call.Arg<IEnumerable<EventToAppendToStorage>>().ToArray();
            _written.AddRange(attempted);
            return Task.FromResult(Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Success(AppendedEventsFrom(attempted)));
        });
    }

    protected Task<AppendResult> AppendProtected() => _eventSequence.AppendToNamespace(Incarnation, _events[0], _correlationId, [], Identity.System, ConcurrencyScope.None);
    protected Task<AppendManyResult> AppendBatchProtected() => _eventSequence.AppendManyToNamespace(Incarnation, _events, _correlationId, [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
}
