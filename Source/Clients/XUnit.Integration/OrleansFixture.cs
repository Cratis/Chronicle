// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ServiceModel;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a fixture for Orleans integration tests.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrleansFixture"/> class.
/// </remarks>
/// <param name="chronicleFixture"><see cref="ChronicleFixture"/> to use.</param>
public class OrleansFixture(ChronicleFixture chronicleFixture) : BaseOrleansFixture(chronicleFixture)
{
    /// <inheritdoc/>
    protected override IAsyncDisposable CreateWebApplicationFactory()
    {
        var startupType = TestAssembly!.ExportedTypes.FirstOrDefault(type => type.Name == "Startup");
        startupType ??= TestAssembly!.ExportedTypes.FirstOrDefault()!;
        var webApplicationFactoryType = typeof(ChronicleWebApplicationFactory<>).MakeGenericType(startupType!);
        var configureServices = ConfigureServices;
        return (Activator.CreateInstance(webApplicationFactoryType, [this, configureServices, ContentRoot]) as IAsyncDisposable)!;
    }

    /// <summary>
    /// Overridable method to configure services.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to configure.</param>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }
}

/// <summary>
/// Represents a fixture for Orleans integration tests.
/// </summary>
/// <typeparam name="TFactory">The web application factory type.</typeparam>
/// <typeparam name="TStartup">The startup class type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="OrleansFixture"/> class.
/// </remarks>
/// <param name="chronicleFixture"><see cref="ChronicleFixture"/> to use.</param>
#pragma warning disable SA1402
public class OrleansFixture<TFactory, TStartup>(ChronicleFixture chronicleFixture) : BaseOrleansFixture(chronicleFixture)
#pragma warning restore SA1402
    where TFactory : IntegrationTestWebApplicationFactory<TStartup>
    where TStartup : class
{
    /// <inheritdoc/>
    protected override IAsyncDisposable CreateWebApplicationFactory()
    {
        var webApplicationFactoryType = typeof(TFactory);
        if (!webApplicationFactoryType.GetConstructors().Any(_ => _.GetParameters().FirstOrDefault()?.ParameterType == typeof(ContentRoot)))
        {
            throw new ServiceActivationException("WebApplicationFactory must have a public constructor that only takes ContentRoot parameter");
        }
        return (Activator.CreateInstance(webApplicationFactoryType, [ContentRoot]) as IAsyncDisposable)!;
    }
}
