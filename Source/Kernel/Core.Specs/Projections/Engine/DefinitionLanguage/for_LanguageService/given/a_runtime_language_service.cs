// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.given;

public class a_runtime_language_service : for_ProjectionFactory.when_creating.given.a_child_collection_projection
{
    protected ILanguageService _language;
    protected EventTypeSchema[] _schemas;
    protected IProjection _projection;
    protected string _generated;

    void Establish()
    {
        _language = new LanguageService(new Generator(), CodeGeneration.given.ProjectionCodeGenerators.All());
        _readModel.Schemas[ReadModelGeneration.First] = JsonSchema.FromJson("""
            {"type":"object","properties":{
              "id":{"type":"string"}, "name":{"type":["string","null"]},
              "age":{"type":"integer","format":"int32"}, "active":{"type":"boolean"},
              "total":{"type":"number","format":"decimal"},
              "profile":{"type":"object","properties":{"name":{"type":["string","null"]}}},
              "entries":{"type":"object","additionalProperties":{"type":"string"}},
              "notes":{"type":"array","items":{"type":"object","properties":{
                "id":{"type":"integer"}, "name":{"type":"string"},
                "rank":{"type":["number","null"]},
                "details":{"type":"object","properties":{"rank":{"type":["number","null"]}}}
              }}}
            }}
            """);
        _schemas = [new((EventType)"Changed", EventTypeOwner.Client, EventTypeSource.Code, JsonSchema.FromJson("""
            {"type":"object","properties":{
              "id":{"type":"integer"}, "name":{"type":"string"},
              "age":{"type":"integer"}, "active":{"type":"boolean"},
              "left":{"type":"number"}, "right":{"type":"number"},
              "rank":{"type":["number","null"]}, "entry":{"type":"string"}
            }}
            """))];
    }

    protected async Task Compile(string body)
    {
        var source = "projection Alerts => Alert\n  no automap\n" + string.Join('\n', body.Split('\n').Select(line => "  " + line));
        var definition = _language.Compile(source, ProjectionOwner.Client, [_readModel], _schemas)
            .Match(value => value, errors => throw new InvalidOperationException(string.Join("; ", errors.Errors)));
        _generated = _language.Generate(definition, _readModel);
        _definition = _language.Compile(_generated, ProjectionOwner.Client, [_readModel], _schemas)
            .Match(value => value, errors => throw new InvalidOperationException(string.Join("; ", errors.Errors)));
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);
    }

    protected async Task<Changeset<AppendedEvent, ExpandoObject>> ProjectChild(object content, ExpandoObject state)
    {
        var incoming = Incoming(content);
        var child = _projection.ChildProjections.Single();
        var resolved = await child.GetKeyResolverFor(incoming.Context.EventType)(_eventSequenceStorage, NullSink.Instance, incoming);
        var changes = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, state);
        var context = new ProjectionEventContext(((ResolvedKey)resolved).Key, incoming, changes, child.GetOperationTypeFor(incoming.Context.EventType), false);
        child.OnNext(context);
        return changes;
    }

    static AppendedEvent Incoming(object content) => new(EventContext.EmptyWithEventSourceId("alert") with { EventType = (EventType)"Changed", SequenceNumber = 5, CausedBy = Identity.System }, content.AsExpandoObject());

    protected Changeset<AppendedEvent, ExpandoObject> Project(object content, ExpandoObject state)
    {
        var incoming = Incoming(content);
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, state);
        _projection.OnNext(new ProjectionEventContext(new Key("alert", ArrayIndexers.NoIndexers), incoming, changeset, _projection.GetOperationTypeFor(incoming.Context.EventType), false));
        return changeset;
    }
}
