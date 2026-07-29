// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;
using ContractIExternalServices = Cratis.Chronicle.Contracts.ExternalServices.IExternalServices;
using ExternalServiceDefinition = Cratis.Chronicle.Contracts.ExternalServices.ExternalServiceDefinition;

namespace Cratis.Chronicle.Services.ExternalServices;

/// <summary>
/// Represents an implementation of <see cref="ContractIExternalServices"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for getting external service definitions.</param>
internal sealed class ExternalServices(IStorage storage) : ContractIExternalServices
{
    /// <inheritdoc/>
    public async Task Add(Contracts.ExternalServices.AddExternalServices request, CallContext context = default)
    {
        var externalServices = storage.GetEventStore(request.EventStore).ExternalServices;
        foreach (var externalService in request.ExternalServices)
        {
            await externalServices.Save(externalService.ToKernel());
        }
    }

    /// <inheritdoc/>
    public async Task Remove(Contracts.ExternalServices.RemoveExternalServices request, CallContext context = default)
    {
        var externalServices = storage.GetEventStore(request.EventStore).ExternalServices;
        foreach (var id in request.ExternalServices)
        {
            await externalServices.Delete(new ExternalServiceId(id));
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ExternalServiceDefinition>> GetExternalServices(Contracts.ExternalServices.GetExternalServicesRequest request)
    {
        var definitions = await storage.GetEventStore(request.EventStore).ExternalServices.GetAll();
        return definitions.Select(definition => definition.ToContract());
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ExternalServiceDefinition>> ObserveExternalServices(Contracts.ExternalServices.GetExternalServicesRequest request, CallContext context = default) =>
        storage.GetEventStore(request.EventStore)
            .ExternalServices
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .Select(definitions => definitions.Select(definition => definition.ToContract()).ToList());
}
