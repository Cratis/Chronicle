// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_recreating_a_cleared_nested_object : Specification
{
    ReadModelScenario<RecreatedNestedModel> _scenario;

    void Establish() => _scenario = new ReadModelScenario<RecreatedNestedModel>();

    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(
        new NestedRegistered("key", "First"),
        new NestedClearedEvent("key"),
        new NestedRegistered("key", "Again"));

    [Fact] void should_keep_the_outer_object() => _scenario.Instance!.Outer.ShouldNotBeNull();
    [Fact] void should_recreate_the_inner_object() => _scenario.Instance!.Outer!.Info.ShouldNotBeNull();
    [Fact] void should_set_the_new_name() => _scenario.Instance!.Outer!.Info!.Name.ShouldEqual("Again");
}
