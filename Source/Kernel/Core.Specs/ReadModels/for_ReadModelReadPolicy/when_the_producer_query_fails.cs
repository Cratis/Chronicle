// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReadPolicy;

public class when_the_producer_query_fails : given.a_read_policy
{
    void Establish() => _manager.GetProducersFor(Arg.Any<ReadModelIdentifier>(), Arg.Any<ProjectionId?>())
        .Returns(Task.FromException<IEnumerable<ProjectionDefinition>>(new InvalidOperationException("Storage unavailable")));

    async Task Because() => _error = await Catch.Exception(() => ReadModelReadPolicy.EnsureReadable(_grains, "store", _definition));

    [Fact] void should_not_allow_the_read() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
