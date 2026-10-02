// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.given;

public class an_initialized_row_with_unset_properties : Specification
{
    protected readonly JsonSchema _schema = JsonSchema.FromJson("""
        {"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"},"count":{"type":"integer"},"amount":{"type":"number"},"children":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"}}}}}}
        """);
    protected readonly TypeFormats _typeFormats = new();
    protected readonly Key _key = new("account-1", ArrayIndexers.NoIndexers);
    protected readonly AppendedEvent _event = new(
        EventContext.From("store", "namespace", EventType.Unknown, EventSourceType.Default, "account-1", EventStreamType.All, EventStreamId.Default, 1UL, CorrelationId.NotSet),
        new ExpandoObject());
    protected SqlSinkHarness _harness;
    protected ISink _sink;
    protected ExpandoObject _initial;
    protected IDictionary<string, object?> _stored;

    async Task Establish()
    {
        _harness = new SqlSinkHarness();
        _sink = _harness.CreateSink(new ReadModelDefinition(
            "account",
            "accounts",
            "Account",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "account-projection",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = _schema },
            []));
        await Apply(new PropertyDifference("name", null, "Opened"));
        _initial = (await _sink.FindOrDefault(_key))!;
    }

    protected async Task Apply(params PropertyDifference[] differences)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        Change[] changes = [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), differences)];
        changeset.Changes.Returns(changes);
        await _sink.ApplyChanges(_key, changeset, _event.Context.SequenceNumber);
        _stored = (await _sink.FindOrDefault(_key))!;
    }

    void Destroy() => _harness.Dispose();
}
