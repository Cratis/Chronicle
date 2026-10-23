// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions;

public class when_max_batch_size_is_negative : Specification
{
    ServiceProvider _services;
    Exception? _error;

    void Establish()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "Cratis:Chronicle:ReadModels:MaxBatchSize", "-1" } }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        ChronicleOptions.AddConfiguration(services, configuration);
        _services = services.BuildServiceProvider();
    }

    void Because() => _error = Catch.Exception(() => _services.GetRequiredService<IStartupValidator>().Validate());

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<OptionsValidationException>();
    [Fact] void should_name_the_invalid_setting() => _error!.Message.ShouldContain("ReadModels.MaxBatchSize must be greater than zero.");

    void Destroy() => _services.Dispose();
}
