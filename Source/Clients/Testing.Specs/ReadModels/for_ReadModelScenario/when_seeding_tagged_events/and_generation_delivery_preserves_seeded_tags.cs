// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_tagged_events;

public class and_generation_delivery_preserves_seeded_tags : Specification
{
    Defaults _defaults;
    ReadModelScenario<TaggedModule> _scenario;

    void Establish()
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterEventType<MigrationTopicCreated>().RegisterEventType<MigrationTopicCreatedV1>()
            .RegisterMigration<MigrationTopicCreatedV1ToV2>().RegisterReducer<ModuleReducer>();
        _defaults = new(new InMemoryClientArtifactsProvider(), null, options);
        _scenario = new(null, _defaults);
    }

    Task Because() => _scenario.Given.ForEventSource(EventSourceId.New()).WithTags("original").Events(new MigrationTopicCreatedV1());

    void Destroy()
    {
        _scenario.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }

    [Fact] void should_deliver_the_migrated_content() => _scenario.Instance!.Module.ShouldEqual("global");
    [Fact] void should_deliver_the_current_generation() => _scenario.Instance!.Generation.ShouldEqual("2");
    [Fact] void should_filter_and_deliver_using_original_metadata() => _scenario.Instance!.Tags.ShouldContainOnly("original");

    public record TaggedModule(string Module, string Generation, IReadOnlyList<string> Tags);

    [FilterEventsByTag("original")]
    public class ModuleReducer
    {
        public TaggedModule Apply(MigrationTopicCreated @event, TaggedModule? current, EventContext context) => new(@event.Module, context.EventType.Generation.Value.ToString(), context.Tags.Select(tag => tag.Value).ToArray());
    }
}
