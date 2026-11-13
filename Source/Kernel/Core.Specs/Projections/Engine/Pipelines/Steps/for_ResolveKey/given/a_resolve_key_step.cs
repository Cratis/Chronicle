// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveKey.given;

public class a_resolve_key_step : Specification
{
    protected ResolveKey _resolveKey;
    protected IProjection _projection;
    protected ProjectionEventContext _context;
    protected ProjectionEventContext _result;
    protected EventType _eventType;

    void Establish()
    {
        _resolveKey = new ResolveKey(
            Substitute.For<IEventSequenceStorage>(),
            Substitute.For<ISink>(),
            new TypeFormats(),
            Substitute.For<ILogger<ResolveKey>>());

        _eventType = new EventType("TestEvent", EventTypeGeneration.First);
        var @event = AppendedEvent.EmptyWithEventType(_eventType);

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns(new List<Change>());
        changeset.CurrentState.Returns(new ExpandoObject());

        _context = new ProjectionEventContext(
            Key.Undefined,
            @event,
            changeset,
            ProjectionOperationType.None,
            NeedsInitialState: false);

        _projection = Substitute.For<IProjection>();
        _projection.Identifier.Returns(new ProjectionId("root-projection"));
        _projection.Path.Returns(new ProjectionPath("Root"));
        _projection.TargetReadModelSchema.Returns(JsonSchema.FromJsonAsync(
            """{"type":"object","properties":{"items":{"type":"array","items":{"type":"object","properties":{"id":{"type":"integer","format":"int64"},"name":{"type":"string"}}}}}}""").GetAwaiter().GetResult());
    }

    protected void ResolverReturns(KeyResolverResult result) =>
        _projection.GetKeyResolverFor(_eventType).Returns((_, _, _) => Task.FromResult(result));
}
