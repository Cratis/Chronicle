// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Sequences.given;

public class a_namespace_bound_keyed_endpoint : an_append_endpoint
{
    protected NamespaceIncarnationId _captured;
    protected NamespaceIncarnationId _forwarded;
    protected INamespaceIncarnations _incarnations;
    protected string?[] _keys;
    protected IEnumerable<Concepts.Auditing.Causation> _forwardedCausation;
    protected Exception _error;
    protected Exception _refusal;
    protected IdempotentAppendResult _singleSettlement;
    protected IdempotentAppendManyResult _batchSettlement;
    protected AppendOnceToNamespace _single;
    protected AppendManyOnceToNamespace _batch;
    protected AppendOnceResult _singleResult;
    protected AppendManyOnceResult _batchResult;
    protected DateTimeOffset _occurred;

    void Establish()
    {
        _captured = NamespaceIncarnationId.New();
        _incarnations = Substitute.For<INamespaceIncarnations>();
        var capability = Substitute.For<IHaveNamespaceIncarnations>();
        var lifecycles = Substitute.For<INamespaceLifecycleStorage>();
        capability.NamespaceLifecycles.Returns(lifecycles);
        _incarnations.GetStorage("store").Returns(capability);
        _incarnations.GetCanonical("store", "tenant").Returns(Task.FromResult<EventStoreNamespaceName?>("Tenant"));
        lifecycles.Get("Tenant").Returns(Task.FromResult<NamespaceLifecycle?>(NamespaceLifecycle.Open("Tenant", NamespaceIncarnationId.New(), DateTimeOffset.UtcNow)));
        _singleSettlement = new(new EventSequences.AppendResult { SequenceNumber = 7 }, AppendOutcome.AlreadyAppended);
        _batchSettlement = new(new EventSequences.AppendManyResult { SequenceNumbers = [7, 8] }) { Outcomes = [AppendOutcome.AlreadyAppended, AppendOutcome.Committed] };
        _eventSequence.AppendWithIdempotencyKeyToNamespace(default!, default!, default!, default!, default!, default!, default!).ReturnsForAnyArgs(call =>
        {
            if (_refusal is not null) throw _refusal;
            _forwarded = call.ArgAt<NamespaceIncarnationId>(0);
            _appendedEvents = [call.ArgAt<EventSequences.EventToAppend>(1)];
            _keys = [call.ArgAt<IdempotencyKey>(2).Value];
            _forwardedCausation = call.ArgAt<IEnumerable<Concepts.Auditing.Causation>>(4);
            return _singleSettlement;
        });
        _eventSequence.AppendManyWithIdempotencyKeysToNamespace(default!, default!, default!, default!, default!, default!, default!).ReturnsForAnyArgs(call =>
        {
            if (_refusal is not null) throw _refusal;
            _forwarded = call.ArgAt<NamespaceIncarnationId>(0);
            _appendedEvents = call.ArgAt<IEnumerable<EventSequences.EventToAppend>>(1).ToArray();
            _keys = call.ArgAt<IReadOnlyList<IdempotencyKey?>>(2).Select(key => key?.Value).ToArray();
            return _batchSettlement;
        });
        _eventSequence.AppendWithIdempotencyKey(default!, default!, default!, default!, default!, default!).ReturnsForAnyArgs(_ => _singleSettlement);
        _eventSequence.AppendManyWithIdempotencyKeys(default!, default!, default!, default!, default!, default!).ReturnsForAnyArgs(_ => _batchSettlement);
        _occurred = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _single = new("store", "tenant", _captured, "event-log", "source", "source-type", "stream-type", "stream", new("event", 1, false), "{}", "key", [new("account", "one")], Tags: ["tag"], Occurred: _occurred, Causation: [new(_occurred, "cause", new Dictionary<string, string>())], EventSource: "definition");
        KeyedEventForEventSourceIdWithNamedTags[] events =
        [
            new(new("source", "source-type", "stream-type", "stream", new("event", 1, false), "{}", ["tag"], [new("account", "one")], _occurred, "subject", [new(_occurred, "cause", new Dictionary<string, string>())], "definition"), "key"),
            new(new("other", "source-type", "stream-type", "stream", new("event", 1, false), "{}", null, []))
        ];
        _batch = new("store", "tenant", _captured, "event-log", events, Tags: ["batch-tag"]);
        _eventSequence.ClearReceivedCalls();
        _incarnations.ClearReceivedCalls();
    }

    protected async Task AppendSingle() => _error = await Catch.Exception(async () => _singleResult = await _single.Handle(_grainFactory, _causation, _principal, _incarnations));
    protected async Task AppendBatch() => _error = await Catch.Exception(async () => _batchResult = await _batch.Handle(_grainFactory, _causation, _principal, _incarnations));
}
