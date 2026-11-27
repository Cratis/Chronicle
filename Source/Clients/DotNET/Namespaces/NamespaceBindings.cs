// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Queries;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Namespaces;

/// <summary>Reads namespace identities without ever ensuring or recreating a namespace.</summary>
/// <param name="eventStore">The event store.</param>
/// <param name="servicesAccessor">The kernel services.</param>
internal class NamespaceBindings(EventStoreName eventStore, IChronicleServicesAccessor servicesAccessor) : INamespaceBindings
{
    /// <inheritdoc/>
    public async Task<NamespaceBinding> LookupNamespaceBinding(EventStoreNamespaceName @namespace, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NamespaceBindingResponse response;
        try
        {
            var services = await servicesAccessor.GetServices(cancellationToken);
            response = await services.Namespaces.LookupNamespaceBinding(
                new LookupNamespaceBindingRequest { EventStore = eventStore, Namespace = @namespace },
                new CallContext(new CallOptions(cancellationToken: cancellationToken))).EnsureSuccess();
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.Unimplemented)
        {
            throw new NamespaceBindingsNotSupported();
        }

        return ToBinding(response);
    }

    NamespaceBinding ToBinding(NamespaceBindingResponse response)
    {
        var status = (NamespaceBindingStatus)response.Status;
        if (response.ProtocolVersion != 1) throw new NamespaceBindingsNotSupported();
        if (!Enum.IsDefined(status)) throw new NamespaceBindingsNotSupported();
        if (string.IsNullOrEmpty(response.Namespace)) throw new NamespaceBindingsNotSupported();

        if (status == NamespaceBindingStatus.Open && response.Incarnation.GetValueOrDefault() == Guid.Empty)
        {
            throw new NamespaceBindingsNotSupported();
        }

        NamespaceIncarnationId? identity = status == NamespaceBindingStatus.Open ? new(response.Incarnation!.Value) : null;
        return new(eventStore, response.Namespace, identity, status, response.ProtocolVersion);
    }
}
