// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions;

public class when_named_sinks_are_invalid : Specification
{
    ServiceProvider _services;
    Exception? _error;

    void Establish()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Cratis:Chronicle:Sinks:Configurations:reporting:Type", "SQL" }
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        ChronicleOptions.AddConfiguration(services, configuration);
        _services = services.BuildServiceProvider();
    }

#pragma warning disable SYSLIB0066 // The synchronous IStartupValidator is what ValidateOnStart registers and the spec exercises.
    void Because() => _error = Catch.Exception(() => _services.GetRequiredService<IStartupValidator>().Validate());
#pragma warning restore SYSLIB0066

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<OptionsValidationException>();
    [Fact] void should_name_the_configuration() => _error.Message.ShouldContain("'reporting'");

    void Destroy() => _services.Dispose();
}
