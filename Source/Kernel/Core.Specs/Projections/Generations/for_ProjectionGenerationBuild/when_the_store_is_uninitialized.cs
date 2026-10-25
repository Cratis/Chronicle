// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations.for_ProjectionGenerationBuild;

/// <summary>A missing generation floor refuses admission before reservation.</summary>
public class when_the_store_is_uninitialized : given.a_generation_build_owner
{
    /// <summary>Models a store whose generation floor is not established.</summary>
    void Establish() => _keys.For(Store, _key.Namespace, _key.Projection).Returns(_ => throw new GenerationFloorNotEstablished(Store));

    /// <summary>Attempts owner admission.</summary>
    async Task Because() => _result = await Start();

    /// <summary>The owner reports the missing floor.</summary>
    [Fact] void should_be_uninitialized() => _result.Result.ShouldEqual(GenerationBuildAdmissionResult.Uninitialized);

    /// <summary>No candidate is reserved.</summary>
    [Fact] async Task should_leave_only_the_serving_generation() => (await Binding()).Generations.ShouldContainOnly(_first);

    /// <summary>No writer authority is returned.</summary>
    [Fact] void should_not_return_a_grant() => _result.Grant.ShouldBeNull();
}
