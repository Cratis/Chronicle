// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_with_version;

public class and_a_subscribe_pass_resumes_after_a_newer_registration : given.a_projections_manager_grain
{
    readonly TaskCompletionSource _passPaused = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<IEnumerable<ReadModelDefinition>> _resumePass = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ProjectionDefinition _stored;
    ProjectionDefinition _newer;

    void Establish()
    {
        _stored = CreateDefinition("the-projection", "the-read-model") with { Version = "2.0.0-rc.2" };
        _newer = _stored with { Version = "2.0.0-rc.10", Tags = ["Newer"] };
        _state.Projections = [_stored];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
        var calls = 0;
        _readModelsManager.GetDefinitions().Returns(_ =>
        {
            if (Interlocked.Increment(ref calls) != 1) return Task.FromResult(_readModelDefinitions);
            _passPaused.TrySetResult();
            return _resumePass.Task;
        });
    }

    async Task Because()
    {
        var pass = _silo.TimerRegistry.FireAllAsync();
        try
        {
            await _passPaused.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _grain.Register([_newer], version: _newer.Version).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _resumePass.TrySetResult(_readModelDefinitions);
            await pass.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    [Fact] void should_keep_the_newer_registered_definition() => _state.Projections.ShouldContainOnly(_newer);
    [Fact] async Task should_never_send_the_captured_stale_definition() => await _projectionGrain.DidNotReceive().SetDefinition(_stored);
    [Fact] async Task should_send_the_current_definition_from_both_passes() => await _projectionGrain.Received(2).SetDefinition(_newer);
}
