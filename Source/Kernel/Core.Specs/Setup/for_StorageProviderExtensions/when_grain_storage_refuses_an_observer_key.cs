// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;

namespace Cratis.Chronicle.Setup.for_StorageProviderExtensions;

public class when_grain_storage_refuses_an_observer_key : Specification
{
    ServiceProvider _services;
    ResiliencePipeline _pipeline;
    ObserverKeyDoesNotMatchDefinition _refusal;
    Exception _result;
    int _attempts;

    void Establish()
    {
        var services = new ServiceCollection();
        var siloBuilder = Substitute.For<ISiloBuilder>();
        siloBuilder.Services.Returns(services);
        siloBuilder.Configuration.Returns(new ConfigurationBuilder().Build());
        siloBuilder.AddStorageProviders();
        services.Configure<ResilientStorageOptions>(ResilientGrainStorage.ResiliencePipelineKey, options =>
        {
            options.Retry.Delay = TimeSpan.Zero;
            options.Retry.MaxRetryAttempts = 1;
        });
        _services = services.BuildServiceProvider();
        _pipeline = _services.GetRequiredService<ResiliencePipelineProvider<string>>().GetPipeline(ResilientGrainStorage.ResiliencePipelineKey);
        _refusal = new(new("observer", "store", "namespace", "log"), "inbox");
    }

    async Task Because() => _result = await Catch.Exception(async () => await _pipeline.ExecuteAsync(_ =>
    {
        _attempts++;
        return ValueTask.FromException(_refusal);
    }));

    void Destroy() => _services.Dispose();

    [Fact] void should_not_retry_the_refusal() => _attempts.ShouldEqual(1);
    [Fact] void should_preserve_the_refusal() => _result.ShouldEqual(_refusal);
}
