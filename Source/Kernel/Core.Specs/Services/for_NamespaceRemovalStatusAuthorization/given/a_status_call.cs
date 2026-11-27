// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.Extensions.Logging.Abstractions;

using NamespaceService = Cratis.Chronicle.Services.Namespaces.Namespaces;

namespace Cratis.Chronicle.Services.for_NamespaceRemovalStatusAuthorization.given;

public class a_status_call : for_StoreEntitlementEnforcement.given.a_store_entitlement_caller
{
    protected INamespaceIncarnations _incarnations;
    protected INamespaceCasing _casing;
    protected INamespaceLifecycleStorage _lifecycle;
    protected NamespaceLifecycle _state;
    protected Contracts.Namespaces.INamespaces _service;
    protected Contracts.Queries.QueryResult<Contracts.Namespaces.NamespaceRemovalStatusResponse> _result;

    void Establish()
    {
        _state = NamespaceLifecycle.Open("tenant", NamespaceIncarnationId.New(), DateTimeOffset.UtcNow)
            .BeginRemoval(Guid.NewGuid(), DateTimeOffset.UtcNow)
            .RequireRecovery("NamespaceRemovalBudgetExhausted");
        _lifecycle = Substitute.For<INamespaceLifecycleStorage>();
        _lifecycle.Get(Arg.Any<EventStoreNamespaceName>()).Returns(Task.FromResult<NamespaceLifecycle?>(_state));
        var capability = Substitute.For<IHaveNamespaceIncarnations>();
        capability.NamespaceLifecycles.Returns(_lifecycle);
        _incarnations = Substitute.For<INamespaceIncarnations>();
        _incarnations.GetStorage("shop").Returns(capability);
        _casing = Substitute.For<INamespaceCasing>();
        _casing.Canonicalize(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(call => call.ArgAt<EventStoreNamespaceName>(1));
        _service = new NamespaceService(Substitute.For<ICommandPipeline>(), _incarnations, Substitute.For<IStorage>(), _casing, NullLogger<NamespaceService>.Instance);
    }

    protected async Task Inspect() => _result = await _service.GetNamespaceRemovalStatus(new() { EventStore = "shop", Namespace = "tenant" }, ContextFor(true));
}
