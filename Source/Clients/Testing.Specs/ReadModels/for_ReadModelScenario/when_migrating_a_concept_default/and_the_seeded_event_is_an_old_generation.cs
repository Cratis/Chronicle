// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_migrating_a_concept_default.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_migrating_a_concept_default;

public class and_the_seeded_event_is_an_old_generation : Specification
{
    ReadModelScenario<ModuleTopic> _scenario;

    void Establish() => _scenario = new();
    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ModuleTopicCreatedV1());
    void Destroy() => _scenario.Dispose();

    [Fact] void should_materialize_the_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_materialize_the_concept_default() => _scenario.Instance!.Module.ShouldEqual(new ModuleKey("global"));
    [Fact] void should_hold_the_plain_value() => _scenario.Instance!.Module.Value.ShouldEqual("global");
}
