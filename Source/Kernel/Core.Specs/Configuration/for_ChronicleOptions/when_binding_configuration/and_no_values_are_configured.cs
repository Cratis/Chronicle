// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

public class and_no_values_are_configured : Specification
{
    ChronicleOptions _options;

    void Establish()
    {
        _options = new ChronicleOptions();
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build()
            .GetSection(ChronicleOptions.SectionPath)
            .Bind(_options);
    }

    [Fact] void should_default_the_revise_interval() => _options.ConnectedClients.ReviseIntervalSeconds.ShouldEqual(2);
    [Fact] void should_default_the_reservation_ttl() => _options.ConnectedClients.ReservationTtlSeconds.ShouldEqual(30);
    [Fact] void should_default_the_stale_threshold() => _options.ConnectedClients.StaleThresholdSeconds.ShouldEqual(5);
    [Fact] void should_default_the_observe_interval() => _options.ConnectedClients.ObserveIntervalSeconds.ShouldEqual(1);
    [Fact] void should_default_the_keep_alive_interval() => _options.ConnectedClients.KeepAliveIntervalSeconds.ShouldEqual(1);
    [Fact] void should_default_the_retry_delay() => _options.Webhooks.RetryDelaySeconds.ShouldEqual(2);
    [Fact] void should_default_the_circuit_breaker_sampling_duration() => _options.Webhooks.CircuitBreakerSamplingDurationSeconds.ShouldEqual(30);
    [Fact] void should_default_the_circuit_breaker_break_duration() => _options.Webhooks.CircuitBreakerBreakDurationSeconds.ShouldEqual(15);
    [Fact] void should_default_the_request_timeout() => _options.Webhooks.RequestTimeoutSeconds.ShouldEqual(60);
    [Fact] void should_default_the_webhook_test_timeout() => _options.Webhooks.TestTimeoutSeconds.ShouldEqual(10);
    [Fact] void should_default_the_live_query_poll_interval() => _options.Sql.LiveQueryPollIntervalSeconds.ShouldEqual(2);
    [Fact] void should_default_the_subscription_ready_timeout() => _options.Observers.SubscriptionReadyTimeout.ShouldEqual(5);
    [Fact] void should_default_the_queue_depletion_wait_timeout() => _options.Events.QueueDepletionWaitTimeoutMilliseconds.ShouldEqual(500);
    [Fact] void should_default_the_health_port_to_not_set() => _options.Health.Port.ShouldBeNull();
    [Fact] void should_default_health_tls_to_enabled() => _options.Health.Tls.ShouldBeTrue();
    [Fact] void should_default_health_exclusive_to_disabled() => _options.Health.Exclusive.ShouldBeFalse();
}
