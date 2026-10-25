// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.for_ProjectionGenerationBuild;

/// <summary>A fresh owner activation recovers the same durable reservation and grant.</summary>
public class when_starting_the_same_operation_twice : given.a_generation_build_owner
{
    /// <summary>The admission returned by the first activation.</summary>
    GenerationBuildAdmission _initial;

    /// <summary>Starts the same immutable operation on two owner activations.</summary>
    async Task Because()
    {
        _initial = await Start();
        _result = await Start();
    }

    /// <summary>The first start exercises actual admission.</summary>
    [Fact] void should_admit_the_first_start() => _initial.Result.ShouldEqual(GenerationBuildAdmissionResult.Admitted);

    /// <summary>The repeated request recovers the complete admission.</summary>
    [Fact] void should_return_the_same_admission() => _result.ShouldEqual(_initial);

    /// <summary>No new grant epoch is minted on retry.</summary>
    [Fact] void should_keep_the_fixed_grant_epoch() => _result.Grant!.Epoch.ShouldEqual(1L);

    /// <summary>No third generation is allocated.</summary>
    [Fact] async Task should_reserve_only_one_candidate() => (await Binding()).Generations.Count.ShouldEqual(2);

    /// <summary>The active serving generation is unchanged.</summary>
    [Fact] async Task should_preserve_the_serving_destination() => (await Binding()).Destination.ShouldEqual(_first.Identity.Destination);
}
