// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.for_ProjectionGenerationBuild;

/// <summary>The disabled flag refuses admission without touching storage.</summary>
public class when_generations_are_disabled : given.a_generation_build_owner
{
    /// <summary>Disables generations before the request.</summary>
    void Establish() => _enablement.Enabled.Returns(false);

    /// <summary>Attempts owner admission.</summary>
    async Task Because() => _result = await Start();

    /// <summary>The disabled path returns its explicit refusal.</summary>
    [Fact] void should_be_disabled() => _result.Result.ShouldEqual(GenerationBuildAdmissionResult.Disabled);

    /// <summary>The disabled path never resolves storage-backed keys.</summary>
    [Fact] void should_not_resolve_a_key() => _keys.ReceivedCalls().ShouldBeEmpty();

    /// <summary>The disabled path never reaches the observer storage graph.</summary>
    [Fact] void should_not_reach_storage() => _storage.ReceivedCalls().ShouldBeEmpty();

    /// <summary>No candidate or writer grant is created.</summary>
    [Fact] async Task should_leave_only_the_serving_generation() => (await Binding()).Generations.ShouldContainOnly(_first);

    /// <summary>No writer authority is returned.</summary>
    [Fact] void should_not_return_a_grant() => _result.Grant.ShouldBeNull();
}
