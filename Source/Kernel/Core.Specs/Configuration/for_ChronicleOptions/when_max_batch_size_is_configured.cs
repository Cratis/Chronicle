// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions;

public class when_max_batch_size_is_configured : Specification
{
    ServiceProvider _services;
    Exception? _error;

    void Establish()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "Cratis:Chronicle:ReadModels:MaxBatchSize", "3" } }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        ChronicleOptions.AddConfiguration(services, configuration);
        _services = services.BuildServiceProvider();
    }

    void Because() => _error = Catch.Exception(() => _services.GetRequiredService<IStartupValidator>().Validate());

    [Fact] void should_accept_startup() => _error.ShouldBeNull();
    [Fact] void should_use_the_expected_cap() => _services.GetRequiredService<IOptions<ChronicleOptions>>().Value.ReadModels.MaxBatchSize.ShouldEqual(3);

    void Destroy() => _services.Dispose();
}
