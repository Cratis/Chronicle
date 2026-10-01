// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.given;

public class an_isolated_publication : Specification
{
    protected ISinkCollections _collections;
    protected ISink _target;
    protected Sink _sink;
    protected ReplayContext _context;
    void Establish()
    {
        _collections = Substitute.For<ISinkCollections>();
        _target = Substitute.For<ISink>();
        var model = new ReadModelDefinition("model", "Model", "Model", ReadModelOwner.None, ReadModelSource.Code, ReadModelObserverType.Reducer, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema>(), []);
        _sink = new(model, Substitute.For<IMongoDBConverter>(), _collections, Substitute.For<IChangesetConverter>(), Substitute.For<IExpandoObjectConverter>(), Substitute.For<IReadModelChangeStreams>());
        _context = new(new("model", 1), "Model", "Revert", DateTimeOffset.UtcNow) { ReplayContainerName = "isolated", AllowEmptyResult = true };
    }
}
