// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Statistics;

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjections;

public class when_lowering_the_statistics_projection_twice : Engine.for_ProjectionDefinitionComparer.given.a_projection_definition_comparer
{
    ProjectionDefinitionCompareResult _result;

    void Establish() => _projectionsStorage.Has(Arg.Any<ProjectionId>()).Returns(true);

    async Task Because()
    {
        var first = KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection)).Projections.ToArray();
        var second = KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection)).Projections.ToArray();
        _result = await _comparer.Compare(_projectionKey, first[0], second[0]);
    }

    [Fact] void should_have_the_same_definition() => _result.ShouldEqual(ProjectionDefinitionCompareResult.Same);
}
