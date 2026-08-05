// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_Projections.when_asking_for_failed_partitions;

/// <summary>
/// The model-keyed sibling of the projection-keyed method, so a caller does not have to know that the untyped
/// overload happens to accept a read model type as well.
/// </summary>
public class of_a_model_bound_projection : given.a_discovered_model_bound_projection
{
    static readonly FailedPartition _failedPartition = new(Guid.NewGuid(), "the-projection", "the-partition", []);

    IEnumerable<FailedPartition> _byModelType;
    IEnumerable<FailedPartition> _byModelTypeArgument;

    void Establish() => _failedPartitions.GetFailedPartitionsFor(Arg.Any<ObserverId>()).Returns([_failedPartition]);

    async Task Because()
    {
        _byModelType = await _projections.GetFailedPartitionsForModel<TheModelBoundReadModel>();
#pragma warning disable CA2263 // the untyped overload is the thing under test here
        _byModelTypeArgument = await _projections.GetFailedPartitionsForModel(typeof(TheModelBoundReadModel));
#pragma warning restore CA2263
    }

    [Fact] void should_ask_for_the_failed_partitions_of_that_projection() =>
        _failedPartitions.Received().GetFailedPartitionsFor(_projections.GetProjectionIdForModel<TheModelBoundReadModel>().Value);

    [Fact] void should_return_them() => _byModelType.ShouldContainOnly([_failedPartition]);
    [Fact] void should_answer_the_same_through_the_type_argument() => _byModelTypeArgument.ShouldContainOnly([_failedPartition]);
}
