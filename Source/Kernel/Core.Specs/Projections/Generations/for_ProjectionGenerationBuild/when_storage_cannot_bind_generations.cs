// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_ProjectionGenerationBuild;

/// <summary>A SQL-style storage refusal is mapped without admitting a writer.</summary>
public class when_storage_cannot_bind_generations : given.a_generation_build_owner
{
    /// <summary>Models the provider's explicit lack of generation support.</summary>
    void Establish()
    {
        _builds = Substitute.For<IGenerationBuildStorage>();
        _builds.ReserveNext(Store, _key, _request.Execution, _request.Operation, _request.At).Returns(Task<GenerationReservation> (_) => throw new MaterializationBindingsNotSupported());
    }

    /// <summary>Attempts owner admission.</summary>
    async Task Because() => _result = await Start();

    /// <summary>The provider refusal is explicit.</summary>
    [Fact] void should_be_unsupported() => _result.Result.ShouldEqual(GenerationBuildAdmissionResult.Unsupported);

    /// <summary>No candidate is written.</summary>
    [Fact] async Task should_leave_only_the_serving_generation() => (await Binding()).Generations.ShouldContainOnly(_first);

    /// <summary>No writer authority is returned.</summary>
    [Fact] void should_not_return_a_grant() => _result.Grant.ShouldBeNull();
}
