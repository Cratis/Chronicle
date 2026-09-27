// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_declarative_projection_for_a_discovered_read_model : given.projections_for_explicit_registration
{
    Exception _error;

    async Task Establish()
    {
        _clientArtifacts.ModelBoundProjections.Returns([typeof(RegisteredCustomer)]);
        await _projections.Discover();
    }

    async Task Because() => _error = await Catch.Exception(() => _projections.Register<RegisteredCustomer>(projection => projection.From<CustomerRegistered>(), "competing"));

    [Fact] void should_fail() => _error.ShouldBeOfExactType<ReadModelAlreadyHasProjection>();
}
