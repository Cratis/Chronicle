// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating_observer_retry_overrides;

public class and_settings_are_valid : given.configured_retry_options
{
    [Fact] void should_accept_omitted_overrides() => Validate(new ChronicleOptions()).Length.ShouldEqual(0);
    [Fact] void should_accept_an_empty_entry_inheriting_globals() => Validate(new ObserverRetryOverrides()).Length.ShouldEqual(0);
    [Fact] void should_accept_unlimited_exponential_retries() => Validate(new ObserverRetryOverrides { MaxRetryAttempts = 0, ExponentialBackoffDelayFactor = float.MaxValue }).Length.ShouldEqual(0);
    [Fact] void should_accept_a_cap_smaller_than_the_base() => Validate(new ObserverRetryOverrides { BackoffDelay = 50, MaximumBackoffDelay = 2 }).Length.ShouldEqual(0);
    [Fact] void should_accept_opaque_non_uuid_components_without_trimming() => Validate(new ObserverRetryOverrides(), key: " observer #store#namespace#sequence").Length.ShouldEqual(0);
    [Fact] void should_not_validate_legacy_globals_without_entries() => Validate(new ChronicleOptions { Observers = new() { MaxRetryAttempts = -1, BackoffDelay = 0, MaximumBackoffDelay = 0, ExponentialBackoffDelayFactor = 0 } }).Length.ShouldEqual(0);
}
