// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.given;

public class configured_alert_options : Specification
{
    protected ServiceProvider _services;
#pragma warning disable SYSLIB0066 // The synchronous IStartupValidator is what ValidateOnStart registers and the spec exercises.
    protected IStartupValidator _startupValidator;
#pragma warning restore SYSLIB0066

    protected void Configure(string raiseAfter, string condition = "partition-failing")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Cratis:Chronicle:Alerts:Conditions:{condition}:RaiseAfter"] = raiseAfter
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        ChronicleOptions.AddConfiguration(services, configuration);
        _services = services.BuildServiceProvider();
#pragma warning disable SYSLIB0066 // See the field declaration.
        _startupValidator = _services.GetRequiredService<IStartupValidator>();
#pragma warning restore SYSLIB0066
    }

    void Destroy() => _services.Dispose();
}
