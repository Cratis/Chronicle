// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Sequences.given;

public class a_namespace_bound_batch_endpoint : an_append_endpoint
{
    protected NamespaceIncarnationId _captured;
    protected NamespaceIncarnationId _forwarded;
    protected INamespaceIncarnations _incarnations;
    protected Occurrences.EffectReceipt _receipt;
    protected Concepts.EventSequences.Concurrency.ConcurrencyScopeConjunctions _conjunctions;
    protected Exception _error;

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
        _eventSequence.AppendManyToNamespace(default!, default!, default!, default!, default!, default!).ReturnsForAnyArgs(call => Capture(call));
        _eventSequence.AppendManyWithEffectReceiptToNamespace(default!, default!, default!, default!, default!, default!, default!).ReturnsForAnyArgs(call =>
        {
            _receipt = call.ArgAt<Occurrences.EffectReceipt>(6);
            return Capture(call);
        });
        _eventSequence.AppendManyWithConjunctionsToNamespace(default!, default!, default!, default!, default!, default!).ReturnsForAnyArgs(call =>
        {
            _conjunctions = call.ArgAt<Concepts.EventSequences.Concurrency.ConcurrencyScopeConjunctions>(5);
            return Capture(call);
        });
        _eventSequence.AppendManyWithConjunctionsAndEffectReceiptToNamespace(default!, default!, default!, default!, default!, default!, default!).ReturnsForAnyArgs(call =>
        {
            _conjunctions = call.ArgAt<Concepts.EventSequences.Concurrency.ConcurrencyScopeConjunctions>(5);
            _receipt = call.ArgAt<Occurrences.EffectReceipt>(6);
            return Capture(call);
        });
        _eventSequence.ClearReceivedCalls();
        _incarnations.ClearReceivedCalls();
    }

    EventSequences.AppendManyResult Capture(NSubstitute.Core.CallInfo call)
    {
        _forwarded = call.ArgAt<NamespaceIncarnationId>(0);
        _appendedEvents = call.ArgAt<IEnumerable<EventSequences.EventToAppend>>(1).ToArray();
        return new EventSequences.AppendManyResult();
    }
}
