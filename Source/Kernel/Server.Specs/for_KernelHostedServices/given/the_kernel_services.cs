// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics.OpenTelemetry;
using Cratis.Chronicle.Server.Authentication;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.for_KernelHostedServices.given;

/// <summary>
/// The kernel's service collection, composed with the same registrations the server's Program makes, without starting
/// anything.
/// </summary>
/// <remarks>
/// Storage is in memory and authentication is enabled, so the optional registrations that come with authentication are
/// part of the collection.
/// </remarks>
public class the_kernel_services : Specification
{
    protected IServiceCollection _services;

    void Establish()
    {
        var options = new ChronicleOptions
        {
            Storage = new Configuration.Storage { Type = StorageType.InMemory },
            Authentication = new Configuration.Authentication { Enabled = true, Authority = "https://identity.example" }
        };
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(Options.Create(options));
        builder.Host.UseOrleans(silo => silo
            .UseLocalhostClustering()
            .AddChronicleToSilo(chronicle => chronicle.WithInMemory(options)));
        builder.Services
            .AddBindingsByConvention()
            .AddChronicleTelemetry(builder.Configuration)
            .AddSelfBindings()
            .AddGrpcServices();
        builder.Services.AddChronicleAuthentication(options);
        builder.Services.UseDeferredSiloStart();
        _services = builder.Services;
    }
}
