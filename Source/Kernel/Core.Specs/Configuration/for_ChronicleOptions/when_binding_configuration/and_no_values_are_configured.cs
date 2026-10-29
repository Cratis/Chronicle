// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;
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

#if DEVELOPMENT
    [Fact] void should_default_target_validation_to_warn() => _options.Webhooks.Validation.ShouldEqual(WebhookValidationMode.Warn);
#else
    [Fact] void should_default_target_validation_to_block_local_addresses() => _options.Webhooks.Validation.ShouldEqual(WebhookValidationMode.BlockLocalAddresses);
#endif
    [Fact] void should_not_allow_insecure_http_by_default() => _options.Webhooks.AllowInsecureHttp.ShouldBeFalse();
    [Fact] void should_have_no_approved_hosts_by_default() => _options.Webhooks.AllowedHosts.ShouldBeEmpty();
    [Fact] void should_have_no_event_store_host_overrides_by_default() => _options.Webhooks.EventStoreAllowedHosts.ShouldBeEmpty();
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
    [Fact] void should_disable_native_postgresql_entra_authentication() => _options.Storage.PostgreSqlEntra.Enabled.ShouldBeFalse();
    [Fact] void should_not_select_a_postgresql_managed_identity_client_id() => _options.Storage.PostgreSqlEntra.ManagedIdentityClientId.ShouldBeNull();
    [Fact] void should_default_the_live_query_poll_interval() => _options.Sql.LiveQueryPollIntervalSeconds.ShouldEqual(2);
    [Fact] void should_default_the_subscription_ready_timeout() => _options.Observers.SubscriptionReadyTimeout.ShouldEqual(5);
    [Fact] void should_default_the_queue_depletion_wait_timeout() => _options.Events.QueueDepletionWaitTimeoutMilliseconds.ShouldEqual(500);
    [Fact] void should_default_causation_property_retention() => _options.Events.CausationPropertyRetention.ShouldEqual(CausationPropertyRetention.Retain);
    [Fact] void should_default_the_health_port_to_not_set() => _options.Health.Port.ShouldBeNull();
    [Fact] void should_default_health_tls_to_enabled() => _options.Health.Tls.ShouldBeTrue();
    [Fact] void should_default_health_exclusive_to_disabled() => _options.Health.Exclusive.ShouldBeFalse();
}
