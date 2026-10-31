// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Sequences.for_Revise.when_handling;

public class and_a_chronicle_user_supplies_another_identity : given.an_append_endpoint
{
    IStorage _storage;
    IJsonSchemaMetadataManager _metadata;

    async Task Establish()
    {
        _principal.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "caller"), new Claim(ChronicleClaims.PrincipalKind, ChronicleClaims.User)], "Chronicle")));
        _storage = Substitute.For<IStorage>();
        _metadata = Substitute.For<IJsonSchemaMetadataManager>();
        var eventType = new Concepts.Events.EventType("event", 1);
        var context = Concepts.Events.EventContext.From("store", "namespace", eventType, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 0UL, CorrelationId.New());
        var schema = await JsonSchema.FromJsonAsync("{}");
        _storage.GetEventStore("store").GetNamespace("namespace").GetEventSequence(EventSequenceId.Log).GetEventAt(0UL).Returns(new Concepts.Events.AppendedEvent(context, new ExpandoObject()));
        _storage.GetEventStore("store").EventTypes.GetFor(eventType.Id, eventType.Generation).Returns(new EventTypeSchema(eventType, EventTypeOwner.Server, EventTypeSource.Code, schema));
        _metadata.Apply("store", "namespace", schema, "source", Arg.Any<JsonObject>()).Returns(new JsonObject());
    }

    async Task Because() => await new Revise("store", "namespace", EventSequenceId.Log, 0, new("event", 1, false), "{}", CausedBy: new("other", "Other", "other", null))
        .Handle(_grainFactory, _causation, _principal, _storage, _metadata, CorrelationId.New());

    [Fact]
    void should_record_the_caller() => ((Concepts.Identities.Identity)_eventSequence.ReceivedCalls()
        .Single(call => call.GetMethodInfo().Name == nameof(IEventSequence.Append)).GetArguments()[4]).Subject.ShouldEqual("caller");
}
