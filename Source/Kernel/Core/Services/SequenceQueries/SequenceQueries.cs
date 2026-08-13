// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;
using ContractISequenceQueries = Cratis.Chronicle.Contracts.SequenceQueries.ISequenceQueries;
using SequenceQueryDefinition = Cratis.Chronicle.Contracts.SequenceQueries.SequenceQueryDefinition;

namespace Cratis.Chronicle.Services.SequenceQueries;

/// <summary>
/// Represents an implementation of <see cref="ContractISequenceQueries"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for getting saved queries.</param>
internal sealed class SequenceQueries(IStorage storage) : ContractISequenceQueries
{
    /// <inheritdoc/>
    public async Task<IEnumerable<SequenceQueryDefinition>> GetSequenceQueries(Contracts.SequenceQueries.GetSequenceQueriesRequest request)
    {
        var definitions = await storage.GetEventStore(request.EventStore).SequenceQueries.GetAllFor(request.Owner);
        return definitions.Select(_ => _.ToContract());
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<SequenceQueryDefinition>> ObserveSequenceQueries(
        Contracts.SequenceQueries.GetSequenceQueriesRequest request,
        CallContext context = default) =>
        storage.GetEventStore(request.EventStore)
            .SequenceQueries
            .ObserveAllFor(request.Owner)
            .CompletedBy(context.CancellationToken)
            .Select(definitions => definitions.Select(_ => _.ToContract()).ToList());

    /// <inheritdoc/>
    public Task Save(Contracts.SequenceQueries.SaveSequenceQueryRequest request, CallContext context = default) =>
        storage.GetEventStore(request.EventStore).SequenceQueries.Save(request.Query.ToKernel());

    /// <inheritdoc/>
    public Task Delete(Contracts.SequenceQueries.DeleteSequenceQueryRequest request, CallContext context = default) =>
        storage.GetEventStore(request.EventStore).SequenceQueries.Delete(new SequenceQueryId(request.Id));
}
