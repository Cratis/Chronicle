// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_using_explicit_artifacts;

public class and_a_plain_migration_is_nominated : Specification
{
    EventScenario _scenario;
    IImmutableList<AppendedEvent> _events;
    EventTypeGeneration? _appendedGeneration;

    void Establish()
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts
            .RegisterEventType<Previous>(_ => _.WithId("explicit-person").Generation(1))
            .RegisterEventType<Current>(_ => _.WithId("explicit-person").Generation(2))
            .RegisterMigration<Rename>();
        _scenario = new(new Defaults(new InMemoryClientArtifactsProvider(), null, options));
    }

    async Task Because()
    {
        var result = await _scenario.EventLog.Append(EventSourceId.New(), new Previous("old-content"));
        result.ShouldBeSuccessful();
        _events = await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        _appendedGeneration = await _scenario.ReadAppendedGenerationAtRest(EventSequenceNumber.First);
    }

    void Destroy() => _scenario.Dispose();
    [Fact] void should_migrate_real_stored_content_to_the_current_generation() => _events.Single().Content.ShouldEqual(new Current("old-content"));
    [Fact] void should_preserve_the_appended_generation() => _appendedGeneration!.Value.ShouldEqual(1U);
    [Fact] void should_use_the_explicit_identity() => _events.Single().Context.EventType.Id.Value.ShouldEqual("explicit-person");

    public record Previous(string OldName);
    public record Current(string NewName);
    public class Rename
    {
        public void Upcast(IEventMigrationBuilder<Current, Previous> builder) => builder.Properties(_ => _.RenamedFrom(current => current.NewName, previous => previous.OldName));
        public void Downcast(IEventMigrationBuilder<Previous, Current> builder) => builder.Properties(_ => _.RenamedFrom(previous => previous.OldName, current => current.NewName));
    }
}
