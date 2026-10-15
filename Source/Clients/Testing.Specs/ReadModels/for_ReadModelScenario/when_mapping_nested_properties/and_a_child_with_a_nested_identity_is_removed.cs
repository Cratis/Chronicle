// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties;

public class and_a_child_with_a_nested_identity_is_removed : Specification
{
    ReadModelScenario<NestedIdentityChildren> _scenario;
    Guid _firstId;
    Guid _secondId;

    void Establish()
    {
        _firstId = Guid.NewGuid();
        _secondId = Guid.NewGuid();
        _scenario = new ReadModelScenario<NestedIdentityChildren>().WithProjection(builder => builder
            .NoAutoMap()
            .Children(model => model.Children, children => children
                .IdentifiedBy(child => child.Identity.Id)
                .From<PathBriefed>(from => from
                    .UsingKey(e => e.Brief.Id)
                    .UsingParentKeyFromContext(context => context.EventSourceId)
                    .Set(child => child.Identity.Id).To(e => e.Brief.Id)
                    .Set(child => child.Identity.Topic).To(e => e.Brief.Topic))
                .RemovedWith<PathBriefRemoved>(removed => removed
                    .UsingKey(e => e.Id)
                    .UsingParentKeyFromContext(context => context.EventSourceId))));
    }

    Task Because() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(
        new PathBriefed(new(_firstId, "First")),
        new PathBriefed(new(_secondId, "Second")),
        new PathBriefRemoved(_firstId));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_remove_the_matching_child() => _scenario.Instance!.Children.Any(child => child.Identity.Id == _firstId).ShouldBeFalse();
    [Fact] void should_keep_only_the_other_child() => _scenario.Instance!.Children.Count().ShouldEqual(1);
    [Fact] void should_preserve_the_other_child() => _scenario.Instance!.Children.Single().Identity.Topic.ShouldEqual("Second");
}
