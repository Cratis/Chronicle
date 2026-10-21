// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties;

public class and_the_child_key_is_nested : Specification
{
    ReadModelScenario<PathChildren> _scenario;
    Guid _firstId;
    Guid _secondId;

    void Establish()
    {
        _firstId = Guid.NewGuid();
        _secondId = Guid.NewGuid();
        _scenario = new ReadModelScenario<PathChildren>().WithProjection(builder => builder
            .NoAutoMap()
            .Children(model => model.Children, children => children
                .IdentifiedBy(child => child.Id)
                .From<PathBriefed>(from => from
                    .UsingKey(e => e.Brief.Id)
                    .UsingParentKeyFromContext(context => context.EventSourceId)
                    .Set(child => child.Id).To(e => e.Brief.Id)
                    .Set(child => child.Topic).To(e => e.Brief.Topic))));
    }

    Task Because() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(
        new PathBriefed(new(_firstId, "First")),
        new PathBriefed(new(_secondId, "Second")),
        new PathBriefed(new(_firstId, "Updated")));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_keep_two_distinct_children() => _scenario.Instance!.Children.Count().ShouldEqual(2);
    [Fact] void should_update_the_matching_child() => _scenario.Instance!.Children.Single(child => child.Id == _firstId).Topic.ShouldEqual("Updated");
    [Fact] void should_preserve_the_other_child() => _scenario.Instance!.Children.Single(child => child.Id == _secondId).Topic.ShouldEqual("Second");
}
