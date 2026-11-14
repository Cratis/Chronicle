// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned.given;

public class a_pinned_pipeline : a_projection_with_generations
{
    protected ISink _sink;
    protected int _writes;
    protected Dictionary<string, IDictionary<string, object?>> _saved = new(StringComparer.OrdinalIgnoreCase);
    protected List<SinkWriteMode> _writeModes = [];

    void Establish()
    {
        _sink = Substitute.For<ISink>();
        _sink.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), Arg.Any<object>()).Returns(Option<Key>.None());
        _sink.FindOrDefault(Arg.Any<Key>()).Returns(call => _saved.TryGetValue(call.Arg<Key>().ToString(), out var state) ? (ExpandoObject)state : null);
        _sink.ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>())
            .Returns(call =>
            {
                _writes++;
                _writeModes.Add(call.Arg<SinkWriteMode>());
                _saved[call.Arg<Key>().ToString()] = call.Arg<IChangeset<AppendedEvent, ExpandoObject>>().CurrentState;
                return [];
            });
    }

    protected async Task<ProjectionPipeline> CreatePipeline()
    {
        _projection = await _factory.Create("store", "namespace", _definition, _readModel, _schemas);
        var changes = Substitute.For<IChangesetStorage>();
        var steps = new ICanPerformProjectionPipelineStep[]
        {
            new ResolveKey(_sequence, _sink, new TypeFormats(), NullLogger<ResolveKey>.Instance),
            new SetInitialState(_sink, NullLogger<SetInitialState>.Instance),
            new HandleEvent(_sequence, _sink, NullLogger<HandleEvent>.Instance),
            new SaveChanges(_sink, changes, true, NullLogger<SaveChanges>.Instance)
        };
        return new(_projection, _sink, changes, new ObjectComparer(), steps, new ProjectionHandleLock(), Substitute.For<IReplayScopedCache>(), NullLogger<ProjectionPipeline>.Instance);
    }
}
