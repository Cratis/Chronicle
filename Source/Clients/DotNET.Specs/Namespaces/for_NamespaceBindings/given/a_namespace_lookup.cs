// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Namespaces.for_NamespaceBindings.given;

public class a_namespace_lookup : Specification
{
    protected INamespaceBindings _bindings;
    protected INamespaces _namespaces;
    protected NamespaceBindingResponse _response;
    protected NamespaceBinding _binding;
    protected LookupNamespaceBindingRequest _request;
    protected CancellationToken _cancellationToken;
    protected CancellationToken _forwardedCancellation;
    protected Exception _failure;
    protected Exception _error;

    void Establish()
    {
        var accessor = Substitute.For<IChronicleServicesAccessor>();
        var services = Substitute.For<IServices>();
        accessor.Services.Returns(services);
        _namespaces = Substitute.For<INamespaces>();
        services.Namespaces.Returns(_namespaces);
        _response = new()
        {
            Namespace = "Tenant",
            Incarnation = Guid.NewGuid(),
            Status = Contracts.Storage.Namespaces.NamespaceLifecycleStatus.Open,
            ProtocolVersion = 1
        };
        _namespaces.LookupNamespaceBinding(default!, default).ReturnsForAnyArgs(call =>
        {
            _request = call.ArgAt<LookupNamespaceBindingRequest>(0);
            _forwardedCancellation = call.ArgAt<CallContext>(1).CancellationToken;
            return _failure is null
                ? Task.FromResult(new QueryResult<NamespaceBindingResponse> { Data = _response })
                : Task.FromException<QueryResult<NamespaceBindingResponse>>(_failure);
        });
        _bindings = new NamespaceBindings("store", accessor);
        _namespaces.ClearReceivedCalls();
    }

    protected async Task Lookup() => _error = await Catch.Exception(async () => _binding = await _bindings.LookupNamespaceBinding("tenant", _cancellationToken));
}
