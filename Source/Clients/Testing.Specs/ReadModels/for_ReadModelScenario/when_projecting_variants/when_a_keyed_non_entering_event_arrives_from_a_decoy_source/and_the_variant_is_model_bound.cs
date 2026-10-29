// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_variants.when_a_keyed_non_entering_event_arrives_from_a_decoy_source;

public class and_the_variant_is_model_bound : Specification
{
    ReadModelScenario<ModelBoundKeyedPullRequestItem> _scenario;
    Guid _workItemId;
    Guid _decoyId;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<ModelBoundKeyedPullRequestItem>();
        _workItemId = Guid.NewGuid();
        _decoyId = Guid.NewGuid();
        await _scenario.Given.ForEventSource(_workItemId.ToString()).Events(new PullRequestCreated("/pull/1"));
        await _scenario.Given.ForEventSource(_decoyId.ToString()).Events(new PullRequestCreated("/pull/2"));
    }

    async Task Because() => await _scenario.Given.ForEventSource(_decoyId.ToString()).Events(new KeyedBuildCompleted(_workItemId, "green"));

    [Fact] void should_update_the_keyed_work_item() => _scenario.InstanceForEventSourceId(_workItemId.ToString())!.BuildStatus.ShouldEqual("green");
    [Fact] void should_not_update_the_decoy() => string.IsNullOrEmpty(_scenario.InstanceForEventSourceId(_decoyId.ToString())!.BuildStatus).ShouldBeTrue();
    [Fact] void should_not_create_another_instance() => _scenario.Instances.Count.ShouldEqual(2);
}
