// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_SecretNames.when_deciding_on_legacy_names;

public class and_the_event_store_or_namespace_is_not_eligible : given.key_names
{
    string? _dottedEventStore;
    string? _markerLookalikeEventStore;
    string? _underscoredNamespace;

    void Because()
    {
        _dottedEventStore = LegacyOf("my.store", "default", "customer-42");
        _markerLookalikeEventStore = LegacyOf("chronicle-encrypted-value-global-scope", "default", "customer-42");
        _underscoredNamespace = LegacyOf("store", "ns_1", "customer-42");
    }

    [Fact] void should_not_offer_an_earlier_name_for_a_dotted_event_store() => _dottedEventStore.ShouldBeNull();
    [Fact] void should_not_offer_an_earlier_name_for_an_event_store_that_looks_like_the_global_scope() => _markerLookalikeEventStore.ShouldBeNull();
    [Fact] void should_not_offer_an_earlier_name_for_an_underscored_namespace() => _underscoredNamespace.ShouldBeNull();
}
