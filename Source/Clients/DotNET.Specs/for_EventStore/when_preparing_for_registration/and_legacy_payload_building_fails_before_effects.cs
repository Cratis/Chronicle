// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_legacy_payload_building_fails_before_effects : given.an_explicit_store
{
    Exception _failure;
    Exception _first;
    Exception _reconnect;
    int _legacyBuilds;

    async Task Establish()
    {
        _failure = new Exception("legacy payload failure");
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
        _store = CreateStore();
        await _store.DiscoverAll();
        ((IKernelCapabilities)_connection).Capabilities.Returns([]);
        _generator.BeforeGenerate = (_, _, legacy) =>
        {
            if (legacy)
            {
                _legacyBuilds++;
                throw _failure;
            }
        };
    }

    async Task Because()
    {
        _first = await Catch.Exception(_store.RegisterAll);
        _reconnect = await Catch.Exception(_store.RegisterAll);
    }

    [Fact] void should_preserve_the_exact_payload_failure() => ReferenceEquals(_first, _failure).ShouldBeTrue();
    [Fact] void should_reuse_the_failed_verdict_on_reconnect() => ReferenceEquals(_reconnect, _failure).ShouldBeTrue();
    [Fact] void should_not_rebuild_deterministically_failed_payloads() => _legacyBuilds.ShouldEqual(1);
    [Fact] void should_send_no_store_registration_calls() => _services.EventStores.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_send_no_parallel_read_model_registration_calls() => _services.ReadModels.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_send_no_parallel_event_registration_calls() => _services.EventTypes.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_start_background_retries() => _store.PendingBackgroundRegistrationRetry.ShouldBeNull();
}
