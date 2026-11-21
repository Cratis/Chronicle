// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services.Clients;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Server.for_GrpcServiceRegistrations.when_adding_grpc_services;

/// <summary>
/// The kernel registers self bindings before its gRPC services. Every call on a silo must see the same open connections,
/// or a keep-alive cannot find the connection it belongs to and a client the registry lost is never put back (#5603).
/// </summary>
public class after_self_bindings : Specification
{
    ServiceDescriptor _registration;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSelfBindings();
        services.AddGrpcServices();
        _registration = services.Last(_ => _.ServiceType == typeof(OpenConnections));
    }

    [Fact] void should_share_one_instance_of_the_open_connections() => _registration.Lifetime.ShouldEqual(ServiceLifetime.Singleton);
}
