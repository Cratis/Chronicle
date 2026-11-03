// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.given;

public class configured_observer_retry_overrides : Specification
{
    protected ServiceProvider _services;
    protected IStartupValidator _startupValidator;

    protected ChronicleOptions _options => _services.GetRequiredService<IOptions<ChronicleOptions>>().Value;

    protected void Configure(IDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        ChronicleOptions.AddConfiguration(services, configuration);
        _services = services.BuildServiceProvider();
        _startupValidator = _services.GetRequiredService<IStartupValidator>();
    }

    void Destroy() => _services.Dispose();
}
