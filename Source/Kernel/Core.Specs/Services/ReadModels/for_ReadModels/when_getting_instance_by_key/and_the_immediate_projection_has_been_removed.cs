// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_the_immediate_projection_has_been_removed : given.all_dependencies
{
    GetInstanceByKeyResponse _result = null!;

    void Establish()
    {
        _sink.TypeId.Returns(SinkTypeId.None);

        var immediateProjection = Substitute.For<IImmediateProjection>();
        immediateProjection.GetModelInstance().Returns(ProjectionResult.Empty with
        {
            ProjectedEventsCount = 2,
            LastHandledEventSequenceNumber = (EventSequenceNumber)42
        });
        _grainFactory.GetGrain<IImmediateProjection>(Arg.Any<string>()).Returns(immediateProjection);
    }

    async Task Because() => _result = await _service.GetInstanceByKey(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        ReadModelKey = "removed-model"
    });

    [Fact] void should_return_no_read_model() => _result.ReadModel.ShouldEqual("null");
    [Fact] void should_preserve_the_projected_event_count() => _result.ProjectedEventsCount.ShouldEqual(2UL);
    [Fact] void should_preserve_the_last_handled_sequence_number() => _result.LastHandledEventSequenceNumber.ShouldEqual(42UL);
    [Fact] void should_not_run_compliance_release_for_an_absent_model() => _complianceHelper.DidNotReceiveWithAnyArgs().ReleaseJson(default!, default!, default!, default!);
}
