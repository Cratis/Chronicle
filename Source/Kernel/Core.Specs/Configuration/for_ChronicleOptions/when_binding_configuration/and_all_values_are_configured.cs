// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

public class and_all_values_are_configured : Specification
{
    ChronicleOptions _options;

    void Establish()
    {
        _options = new ChronicleOptions();
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Chronicle:ConnectedClients:ReviseIntervalSeconds"] = "11",
                ["Cratis:Chronicle:ConnectedClients:ReservationTtlSeconds"] = "42",
                ["Cratis:Chronicle:ConnectedClients:StaleThresholdSeconds"] = "9",
                ["Cratis:Chronicle:ConnectedClients:ObserveIntervalSeconds"] = "3",
                ["Cratis:Chronicle:ConnectedClients:KeepAliveIntervalSeconds"] = "4",
                ["Cratis:Chronicle:Webhooks:RetryDelaySeconds"] = "7",
                ["Cratis:Chronicle:Webhooks:CircuitBreakerSamplingDurationSeconds"] = "45",
                ["Cratis:Chronicle:Webhooks:CircuitBreakerBreakDurationSeconds"] = "20",
                ["Cratis:Chronicle:Webhooks:RequestTimeoutSeconds"] = "90",
                ["Cratis:Chronicle:Webhooks:TestTimeoutSeconds"] = "15",
                ["Cratis:Chronicle:Sql:LiveQueryPollIntervalSeconds"] = "6",
                ["Cratis:Chronicle:Observers:SubscriptionReadyTimeout"] = "8",
                ["Cratis:Chronicle:Events:QueueDepletionWaitTimeoutMilliseconds"] = "750"
            })
            .Build()
            .GetSection(ChronicleOptions.SectionPath)
            .Bind(_options);
    }

    [Fact] void should_bind_the_revise_interval() => _options.ConnectedClients.ReviseIntervalSeconds.ShouldEqual(11);
    [Fact] void should_bind_the_reservation_ttl() => _options.ConnectedClients.ReservationTtlSeconds.ShouldEqual(42);
    [Fact] void should_bind_the_stale_threshold() => _options.ConnectedClients.StaleThresholdSeconds.ShouldEqual(9);
    [Fact] void should_bind_the_observe_interval() => _options.ConnectedClients.ObserveIntervalSeconds.ShouldEqual(3);
    [Fact] void should_bind_the_keep_alive_interval() => _options.ConnectedClients.KeepAliveIntervalSeconds.ShouldEqual(4);
    [Fact] void should_bind_the_retry_delay() => _options.Webhooks.RetryDelaySeconds.ShouldEqual(7);
    [Fact] void should_bind_the_circuit_breaker_sampling_duration() => _options.Webhooks.CircuitBreakerSamplingDurationSeconds.ShouldEqual(45);
    [Fact] void should_bind_the_circuit_breaker_break_duration() => _options.Webhooks.CircuitBreakerBreakDurationSeconds.ShouldEqual(20);
    [Fact] void should_bind_the_request_timeout() => _options.Webhooks.RequestTimeoutSeconds.ShouldEqual(90);
    [Fact] void should_bind_the_webhook_test_timeout() => _options.Webhooks.TestTimeoutSeconds.ShouldEqual(15);
    [Fact] void should_bind_the_live_query_poll_interval() => _options.Sql.LiveQueryPollIntervalSeconds.ShouldEqual(6);
    [Fact] void should_bind_the_subscription_ready_timeout() => _options.Observers.SubscriptionReadyTimeout.ShouldEqual(8);
    [Fact] void should_bind_the_queue_depletion_wait_timeout() => _options.Events.QueueDepletionWaitTimeoutMilliseconds.ShouldEqual(750);
}
