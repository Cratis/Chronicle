// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a fixture for Orleans integration tests.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChronicleOrleansFixture"/> class.
/// </remarks>
/// <param name="chronicleFixture"><see cref="ChronicleFixture"/> to use.</param>
public class ChronicleOrleansFixture(ChronicleFixture chronicleFixture) : ChronicleClientFixture(chronicleFixture)
{
    /// <inheritdoc/>
    protected override IAsyncDisposable CreateWebApplicationFactory()
    {
        var startupType = TestAssembly!.ExportedTypes.FirstOrDefault(type => type.Name == "Startup");
        startupType ??= TestAssembly!.ExportedTypes.FirstOrDefault()!;
        var webApplicationFactoryType = typeof(ChronicleOrleansWebApplicationFactory<>).MakeGenericType(startupType!);
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