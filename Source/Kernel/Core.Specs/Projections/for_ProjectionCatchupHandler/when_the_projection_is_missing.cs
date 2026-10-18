// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_ProjectionCatchupHandler;

public class when_the_projection_is_missing : Specification
{
    ProjectionCatchupHandler _handler;
    IProjectionPipelineManager _pipelines;
    ObserverDetails _details;

    void Establish()
    {
        _details = new(new("projection", "store", "namespace", EventSequenceId.Log), ObserverType.Projection);
        _pipelines = Substitute.For<IProjectionPipelineManager>();
        var projections = Substitute.For<Engine.IProjectionsManager>();
        projections.GetCurrent(_details.Key.EventStore, _details.Key.Namespace, _details.Key.ObserverId).Returns(Task.FromResult<Engine.IProjection?>(null));
        _handler = new(projections, _pipelines, Substitute.For<IGrainFactory>(), NullLogger<ProjectionCatchupHandler>.Instance);
    }

    [Theory]
    [InlineData("begin")]
    [InlineData("resume")]
    [InlineData("end")]
    async Task should_report_an_error_instead_of_success(string operation)
    {
        var result = await (operation switch
        {
            "begin" => _handler.BeginCatchupFor(_details),
            "resume" => _handler.ResumeCatchupFor(_details),
            _ => _handler.EndCatchupFor(_details)
        });
        result.TryGetError(out _).ShouldBeTrue();
        await _pipelines.DidNotReceiveWithAnyArgs().GetFor(null!, null!, null!);
    }
}
