// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.for_Occurrences.given;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceLifecycle;

public class when_the_host_signals_application_stopping : lifecycle_occurrences
{
    readonly CancellationTokenSource _stopping = new();
    OccurrenceLifecycle _host;

    async Task Establish()
    {
        _options.ActivationScope = ActivationScope.PerProcess;
        await _occurrences.Discover();
        _store.Registration.Returns(RegistrationOutcome.NotRun with { HasRun = true });
        var client = Substitute.For<IChronicleClient>();
        client.Options.Returns(_options);
        client.GetEventStore(_options.EventStore).Returns(_store);
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(_stopping.Token);
        _host = new(client, _artifacts, lifetime, Options.Create(new HostOptions()), NullLogger<OccurrenceLifecycle>.Instance);
        await _host.StartAsync(CancellationToken.None);
    }

    async Task Because()
    {
        await _stopping.CancelAsync();
        await _host.StopAsync(CancellationToken.None);
    }

    void Destroy()
    {
        _host.Dispose();
        _stopping.Dispose();
    }

    [Fact] void should_record_shutdown_once() => _recordings.Count.ShouldEqual(2);
    [Fact] void should_use_the_starting_process_identity() => _recordings[1].ActivationId.ShouldEqual(_recordings[0].ActivationId);
    [Fact] void should_observe_shutdown_delivery_before_stopping() => _observations.Count.ShouldEqual(1);
}
