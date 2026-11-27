// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Events.Constraints.Operator;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_getting_services;

public class and_requesting_the_constraint_indexes_operator : given.a_connection_after_a_successful_handshake
{
    IServices _services;
    IConstraintIndexes _connectedOperator;
    IConstraintIndexes _resolvedOperator;

    void Establish() => _services = ((IChronicleServicesAccessor)_connection).Services;

    void Because()
    {
        using var provider = new ServiceCollection()
            .AddSingleton(_services)
            .AddCratisChronicleServices()
            .BuildServiceProvider();

        _connectedOperator = _services.ConstraintIndexes;
        _resolvedOperator = provider.GetRequiredService<IConstraintIndexes>();
    }

    [Fact] void should_expose_the_operator_on_the_connection() => _connectedOperator.ShouldNotBeNull();
    [Fact] void should_resolve_the_connected_operator_from_dependency_injection() => _resolvedOperator.ShouldBeSame(_connectedOperator);
}
