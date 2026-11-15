// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_SecretNames.when_deciding_on_legacy_names;

public class and_eligible_tuples_are_compared : given.key_names
{
    static readonly (string EventStore, string Namespace, string Identifier)[] _tuples =
    [
        ("store-1", "default", "customer-42"),
        ("store-1", "default", "customer-43"),
        ("store-1", "other", "customer-42"),
        ("store-2", "default", "customer-42"),
        ("store", "1-default", "customer-42"),
        ("store-1", "default", "customer"),
        ("store", "default", "1-customer-42"),
        ("store", "default", SampleGuid)
    ];

    List<string?> _prefixes = [];
    string? _upperCaseNamespace;
    string? _lowerCaseNamespace;

    void Because()
    {
        _prefixes = [.. _tuples.Select(tuple => LegacyOf(tuple.EventStore, tuple.Namespace, tuple.Identifier))];
        _upperCaseNamespace = LegacyOf("store", "Default", "customer-42");
        _lowerCaseNamespace = LegacyOf("store", "default", "customer-42");
    }

    [Fact] void should_offer_an_earlier_name_for_each() => _prefixes.ShouldEachConformTo(prefix => prefix is not null);
    [Fact] void should_give_each_a_different_earlier_name() => _prefixes.Distinct().Count().ShouldEqual(_tuples.Length);

    [Fact] void should_still_share_earlier_names_between_namespaces_that_differ_only_in_case() => _upperCaseNamespace.ShouldEqual(_lowerCaseNamespace);
}
