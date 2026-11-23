// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Sequences.for_AppendToNamespace.given;

public class a_protected_append_endpoint : Sequences.given.an_append_endpoint
{
    protected NamespaceIncarnationId _captured;
    protected NamespaceIncarnationId _forwarded;
    protected EventSequences.EventToAppend _event;
    protected Occurrences.EffectReceipt _receipt;
    protected INamespaceIncarnations _incarnations;
    protected INamespaceLifecycleStorage _lifecycles;
    protected AppendToNamespace _command;
    protected Exception _error;

    void Establish()
    {
        _captured = NamespaceIncarnationId.New();
        _command = new("store", "tenant", _captured, "event-log", "source", "", "", "", new EventType("event", 1, false), "{}");
        _incarnations = Substitute.For<INamespaceIncarnations>();
        var capability = Substitute.For<IHaveNamespaceIncarnations>();
        _lifecycles = Substitute.For<INamespaceLifecycleStorage>();
        capability.NamespaceLifecycles.Returns(_lifecycles);
        _incarnations.GetStorage("store").Returns(capability);
        _incarnations.GetCanonical("store", "tenant").Returns(Task.FromResult<EventStoreNamespaceName?>("Tenant"));
        _lifecycles.Get("Tenant").Returns(Task.FromResult<NamespaceLifecycle?>(NamespaceLifecycle.Open("Tenant", NamespaceIncarnationId.New(), DateTimeOffset.UtcNow)));
        _eventSequence.AppendToNamespace(
            Arg.Do<NamespaceIncarnationId>(value => _forwarded = value),
            Arg.Do<EventSequences.EventToAppend>(value => _event = value),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScope>()).Returns(Task.FromResult(new EventSequences.AppendResult()));
        _eventSequence.AppendManyWithEffectReceiptToNamespace(
            Arg.Do<NamespaceIncarnationId>(value => _forwarded = value),
            Arg.Do<IEnumerable<EventSequences.EventToAppend>>(value => _event = value.Single()),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<ConcurrencyScopes>(),
            Arg.Do<Occurrences.EffectReceipt>(value => _receipt = value)).Returns(Task.FromResult(new EventSequences.AppendManyResult()));
        _eventSequence.ClearReceivedCalls();
        _incarnations.ClearReceivedCalls();
    }

    protected async Task Append() => _error = await Catch.Exception(() => _command.Handle(_grainFactory, _causation, _principal, _incarnations));
}
