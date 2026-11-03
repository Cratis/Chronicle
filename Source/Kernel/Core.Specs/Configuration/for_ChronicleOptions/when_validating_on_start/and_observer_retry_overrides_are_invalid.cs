// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration.for_ChronicleOptions.given;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating_on_start;

public class and_observer_retry_overrides_are_invalid : configured_observer_retry_overrides
{
    const string BadKey = "bad#my-store#Default#event-log";
    const string InfiniteKey = "infinite#my-store#Default#event-log";

    Exception _error;
    IReadOnlyList<string> _failures;

    void Establish() => Configure(new Dictionary<string, string?>
    {
        ["Cratis:Chronicle:ObserverRetryOverrides:observer-only:MaxRetryAttempts"] = "1",
        ["Cratis:Chronicle:ObserverRetryOverrides:a##c#d:MaxRetryAttempts"] = "1",
        [$"Cratis:Chronicle:ObserverRetryOverrides:{BadKey}:MaxRetryAttempts"] = "-1",
        [$"Cratis:Chronicle:ObserverRetryOverrides:{BadKey}:BackoffDelay"] = "0",
        [$"Cratis:Chronicle:ObserverRetryOverrides:{BadKey}:MaximumBackoffDelay"] = "-5",
        [$"Cratis:Chronicle:ObserverRetryOverrides:{BadKey}:ExponentialBackoffDelayFactor"] = "0.5",
        [$"Cratis:Chronicle:ObserverRetryOverrides:{InfiniteKey}:ExponentialBackoffDelayFactor"] = "Infinity"
    });

    void Because()
    {
        _error = Catch.Exception(_startupValidator.Validate);
        _failures = (_error as OptionsValidationException)?.Failures.ToArray() ?? [];
    }

    [Fact] void should_reject_invalid_options_before_starting() => _error.ShouldBeOfExactType<OptionsValidationException>();
    [Fact] void should_report_every_problem() => _failures.Count.ShouldEqual(7);
    [Fact] void should_reject_a_key_without_event_store_namespace_and_event_sequence() => _failures.Any(_ => _.Contains("ObserverRetryOverrides['observer-only'] must be a full observer key", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_a_key_with_an_empty_part() => _failures.Any(_ => _.Contains("ObserverRetryOverrides['a##c#d'] must be a full observer key", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_negative_max_retry_attempts() => _failures.Any(_ => _.Contains($"ObserverRetryOverrides['{BadKey}'].MaxRetryAttempts", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_a_zero_backoff_delay() => _failures.Any(_ => _.Contains($"ObserverRetryOverrides['{BadKey}'].BackoffDelay", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_a_negative_maximum_backoff_delay() => _failures.Any(_ => _.Contains($"ObserverRetryOverrides['{BadKey}'].MaximumBackoffDelay", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_a_backoff_factor_below_one() => _failures.Any(_ => _.Contains($"ObserverRetryOverrides['{BadKey}'].ExponentialBackoffDelayFactor", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_an_infinite_backoff_factor() => _failures.Any(_ => _.Contains($"ObserverRetryOverrides['{InfiniteKey}'].ExponentialBackoffDelayFactor", StringComparison.Ordinal)).ShouldBeTrue();
}
