// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_appending_generations_with_different_pii : Specification
{
    EventScenario _scenario;
    EventSourceId _id;
    JsonObject _firstStored;
    JsonObject _secondStored;
    IImmutableList<AppendedEvent> _readBack;

    void Establish()
    {
        _scenario = new EventScenario();
        _id = EventSourceId.New();
    }

    async Task Because()
    {
        var first = await _scenario.EventLog.Append(_id, new ContactReclassifiedV1("private-old-alias", "public-old-address"));
        first.ShouldBeSuccessful();
        var second = await _scenario.EventLog.Append(_id, new ContactReclassified("public-new-alias", "private-new-address"));
        second.ShouldBeSuccessful();
        _firstStored = JsonNode.Parse(await _scenario.ReadContentAtRest(EventSequenceNumber.First))!.AsObject();
        _secondStored = JsonNode.Parse(await _scenario.ReadContentAtRest(EventSequenceNumber.First + 1))!.AsObject();
        _readBack = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First, _id);
    }

    [Fact] void should_encrypt_the_old_generations_private_field() => Convert.FromBase64String(_firstStored["alias"]!.GetValue<string>()).Take(4).ShouldContainOnly("CENV"u8.ToArray());
    [Fact] void should_keep_the_old_generations_public_field() => _firstStored["address"]!.GetValue<string>().ShouldEqual("public-old-address");
    [Fact] void should_keep_the_new_generations_public_field() => _secondStored["alias"]!.GetValue<string>().ShouldEqual("public-new-alias");
    [Fact] void should_encrypt_the_new_generations_private_field() => Convert.FromBase64String(_secondStored["address"]!.GetValue<string>()).Take(4).ShouldContainOnly("CENV"u8.ToArray());
    [Fact] void should_release_the_old_generation_into_the_current_client_shape() => _readBack[0].Content.ShouldEqual(new ContactReclassified("private-old-alias", "public-old-address"));
    [Fact] void should_preserve_the_stored_generation_in_the_context() => _readBack[0].Context.EventType.Generation.Value.ShouldEqual(1U);
    [Fact] void should_release_the_new_generation() => _readBack[1].Content.ShouldEqual(new ContactReclassified("public-new-alias", "private-new-address"));

    void Destroy() => _scenario.Dispose();
}
