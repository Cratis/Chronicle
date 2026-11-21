// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_foreign_stores;

public class and_the_effective_modes_match : when_opening_namespace_stores.given.a_client_with_seeds
{
    IEventStore[] _stores;
    IEventStore _legacy;
    IEventStore _explicitNamespace;
    IEventStore _anotherNamespace;

    void Establish() => _options.RegistrationMode = RegistrationMode.EventTypesOnly;

    async Task Because()
    {
        _stores = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => _client.GetEventStore("foreign", null, RegistrationMode.EventTypesOnly))));
        _legacy = await _client.GetEventStore("foreign", default);
        _explicitNamespace = await _client.GetEventStore("foreign", EventStoreNamespaceName.Default, RegistrationMode.EventTypesOnly);
        _anotherNamespace = await _client.GetEventStore("foreign", "tenant", RegistrationMode.EventTypesOnly);
    }

    [Fact] void should_single_flight_concurrent_calls() => _stores.Distinct().Count().ShouldEqual(1);
    [Fact] void should_share_with_legacy_overload() => _legacy.ShouldBeSame(_stores[0]);
    [Fact] void should_share_with_resolved_namespace() => _explicitNamespace.ShouldBeSame(_legacy);
    [Fact] void should_separate_namespaces() => _anotherNamespace.ShouldNotBeSame(_legacy);
}
