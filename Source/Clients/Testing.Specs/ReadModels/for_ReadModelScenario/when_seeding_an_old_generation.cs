// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_seeding_an_old_generation : Specification
{
    ReadModelScenario<MigrationTopic> _scenario;

    void Establish() => _scenario = new();
    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new MigrationTopicCreatedV1());
    void Destroy() => _scenario.Dispose();

    [Fact] void should_materialize_the_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_project_the_migrated_value() => _scenario.Instance!.Module.ShouldEqual("global");
}
