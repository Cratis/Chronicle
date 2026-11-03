// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConfigurationForObserverProvider;

public class when_options_change : given.a_provider
{
    Observers _first;
    Observers _replacementDefaults;
    int _snapshotReads;

    void Establish()
    {
        var first = new ChronicleOptions { Observers = _defaults, ObserverRetryOverrides = new Dictionary<string, ObserverRetryOverrides> { [ObserverKey] = new() { MaxRetryAttempts = 2 } } };
        _replacementDefaults = new() { BackoffDelay = 17, MaximumBackoffDelay = 77 };
        var second = new ChronicleOptions { Observers = _replacementDefaults, ObserverRetryOverrides = new Dictionary<string, ObserverRetryOverrides> { [ObserverKey] = new() { MaxRetryAttempts = 3 } } };
        _monitor.CurrentValue.Returns(_ => ++_snapshotReads == 1 ? first : second);
    }

    async Task Because()
    {
        _first = await _provider.GetFor(ObserverKey);
        _result = await _provider.GetFor(ObserverKey);
    }

    [Fact] void should_read_one_snapshot_per_call() => _snapshotReads.ShouldEqual(2);
    [Fact] void should_keep_the_first_projection_unchanged() => (_first.MaxRetryAttempts, _first.BackoffDelay).ShouldEqual((2, 4));
    [Fact] void should_use_the_new_override_and_defaults_together() => (_result.MaxRetryAttempts, _result.BackoffDelay, _result.MaximumBackoffDelay).ShouldEqual((3, 17, 77));
    [Fact] void should_not_mutate_replacement_defaults() => _replacementDefaults.MaxRetryAttempts.ShouldEqual(10);
}
