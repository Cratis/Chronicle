// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventTypes.when_registering;

public class and_previous_generation_is_declared_with_the_generation_for_attribute : given.all_dependencies
{
    [EventType("vibe-published", generation: 2)]
    record VibePublished(bool GroupChatEnabled);

    [EventTypeGenerationFor<VibePublished>(1)]
    record VibePublishedV1(string PublishedAt);

    EventTypes _subject;
    RegisterEventTypesRequest _capturedRequest;

    async Task Establish()
    {
        var v1Schema = await JsonSchema.FromJsonAsync("""{"title":"VibePublishedV1","properties":{"PublishedAt":{"type":"string"}}}""");
        var v2Schema = await JsonSchema.FromJsonAsync("""{"title":"VibePublished","properties":{"GroupChatEnabled":{"type":"boolean"}}}""");

        var migrator = Substitute.For<IEventTypeMigration>();
        migrator.From.Returns(new EventTypeGeneration(1));
        migrator.To.Returns(new EventTypeGeneration(2));

        _clientArtifacts.EventTypes.Returns([typeof(VibePublishedV1), typeof(VibePublished)]);
        _schemaGenerator.Generate(typeof(VibePublishedV1)).Returns(v1Schema);
        _schemaGenerator.Generate(typeof(VibePublished)).Returns(v2Schema);
        _eventTypeMigrators.GetMigratorsFor(typeof(VibePublished)).Returns([migrator]);

        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);

        _eventTypesService
            .When(_ => _.RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>()))
            .Do(call => _capturedRequest = call.Arg<RegisterEventTypesRequest>());
    }

    async Task Because()
    {
        await _subject.Discover();
        await _subject.Register();
    }

    [Fact] void should_send_one_registration() => _capturedRequest.Types.Count().ShouldEqual(1);
    [Fact] void should_register_both_generation_schemas() => _capturedRequest.Types.First().Generations.Count.ShouldEqual(2);
    [Fact] void should_register_the_real_generation_1_schema() =>
        _capturedRequest.Types.First().Generations.Single(_ => _.Generation == 1).Schema.Contains("PublishedAt").ShouldBeTrue();
    [Fact] void should_not_register_an_empty_placeholder_for_generation_1() =>
        _capturedRequest.Types.First().Generations.Single(_ => _.Generation == 1).Schema.ShouldNotEqual("{}");
    [Fact] void should_register_the_real_generation_2_schema() =>
        _capturedRequest.Types.First().Generations.Single(_ => _.Generation == 2).Schema.Contains("GroupChatEnabled").ShouldBeTrue();
}
