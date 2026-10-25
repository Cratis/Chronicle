// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Projections.Generations.for_ProjectionGenerationBuild;

/// <summary>An event-sequence sink cannot build private generations.</summary>
public class when_the_sink_is_an_event_sequence : given.a_generation_build_owner
{
    /// <summary>Captures inputs with an event-sequence sink.</summary>
    void Establish()
    {
        _key = _key with { SinkType = WellKnownSinkTypes.EventSequence };
        var readModel = GenerationExecutions.ReadModel() with { Sink = new(SinkConfigurationId.None, WellKnownSinkTypes.EventSequence) };
        var execution = _specifications.Capture(GenerationExecutions.Definition(), readModel, GenerationExecutions.EventSchemas(), new("sink-v1"));
        _request = _request with { Key = _key, Execution = execution };
    }

    /// <summary>Attempts owner admission.</summary>
    async Task Because() => _result = await Start();

    /// <summary>The sink capability is refused.</summary>
    [Fact] void should_be_unsupported() => _result.Result.ShouldEqual(GenerationBuildAdmissionResult.Unsupported);

    /// <summary>No catalog entry is created for the unsupported sink.</summary>
    [Fact] async Task should_not_reserve_a_generation() => (await _catalog.Get(Store, _key)).ShouldBeNull();

    /// <summary>The existing serving binding is unchanged.</summary>
    [Fact] async Task should_preserve_the_serving_generation() => (await _catalog.Get(Store, GenerationExecutions.Key))!.Generations.ShouldContainOnly(_first);

    /// <summary>No writer authority is returned.</summary>
    [Fact] void should_not_return_a_grant() => _result.Grant.ShouldBeNull();
}
