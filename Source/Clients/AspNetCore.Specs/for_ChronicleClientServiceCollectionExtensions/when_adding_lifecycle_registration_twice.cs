// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

public class when_adding_lifecycle_registration_twice : Specification
{
    ServiceDescriptor[] _hostedServices;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddCratisChronicleClient();
        services.AddCratisChronicleClient();
        _hostedServices = services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService)).ToArray();
    }

    [Fact] void should_register_one_lifecycle_hosted_service() => _hostedServices.Count(descriptor => descriptor.ImplementationType?.Name == "OccurrenceLifecycle").ShouldEqual(1);
}
