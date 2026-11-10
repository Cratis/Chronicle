// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.for_RepeatedFailureLoggerProvider.given;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLoggerProvider;

public class when_throttling_several_providers : Specification
{
    RecordingLoggerProvider _first;
    RecordingLoggerProvider _second;
    ServiceProvider _services;

    void Establish()
    {
        _first = new();
        _second = new();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerProvider>(_first);
        services.AddSingleton<ILoggerProvider>(_second);
        RepeatedFailureLoggerProvider.ThrottleRepeatedFailures(services);
        _services = services.BuildServiceProvider();
    }

    void Because()
    {
        var logger = _services.GetRequiredService<ILoggerFactory>().CreateLogger("Storage");
        for (var i = 0; i < 10; i++)
        {
            logger.ReadingFailed(new TimeoutException(), i);
        }
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_write_the_first_failure_to_the_first_provider() => _first.Entries.Single().Message.ShouldEqual("Reading 0 failed");
    [Fact] void should_write_the_first_failure_to_the_second_provider() => _second.Entries.Single().Message.ShouldEqual("Reading 0 failed");
}
