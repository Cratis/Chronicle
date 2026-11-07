// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Neo4j.for_GraphDatabaseName.when_getting_the_name;

/// <summary>
/// The vectors published with the naming algorithm in Cratis/Screenplay#1159. Cratis Arc holds the same table.
/// </summary>
public class the_published_vectors : Specification
{
    string _bareDefault;
    string _acme;
    string _acmeUpperCase;
    string _lowerCaseDefault;
    string _prefixedDefault;
    string _prefixedAcme;
    string _space;
    string _umlaut;
    string _dot;
    string _underscoreStore;
    string _digitStore;
    string _tooShort;
    string _atTheLimit;
    string _overTheLimit;
    string _emoji;
    string _emptyNamespace;
    string _nullNamespace;

    void Because()
    {
        _bareDefault = GraphDatabaseName.For(null, "Orders", "Default");
        _acme = GraphDatabaseName.For(null, "Orders", "acme");
        _acmeUpperCase = GraphDatabaseName.For(null, "Orders", "Acme");
        _lowerCaseDefault = GraphDatabaseName.For(null, "Orders", "default");
        _prefixedDefault = GraphDatabaseName.For("cratis", "Orders", "Default");
        _prefixedAcme = GraphDatabaseName.For("cratis", "Orders", "acme");
        _space = GraphDatabaseName.For(null, "Orders", "Acme Corp");
        _umlaut = GraphDatabaseName.For(null, "Orders", "Müller");
        _dot = GraphDatabaseName.For(null, "Orders", "acme.eu");
        _underscoreStore = GraphDatabaseName.For(null, "_Orders", "Default");
        _digitStore = GraphDatabaseName.For(null, "1Orders", "Default");
        _tooShort = GraphDatabaseName.For(null, "ab", "Default");
        _atTheLimit = GraphDatabaseName.For(null, "Orders", new string('x', 56));
        _overTheLimit = GraphDatabaseName.For(null, "Orders", new string('x', 60));
        _emoji = GraphDatabaseName.For(null, "Orders", "🙂");
        _emptyNamespace = GraphDatabaseName.For(null, "Orders", string.Empty);
        _nullNamespace = GraphDatabaseName.For(null, "Orders", null);
    }

    [Fact] void should_use_the_bare_store_name_for_the_default_namespace() => _bareDefault.ShouldEqual("orders");
    [Fact] void should_append_the_namespace() => _acme.ShouldEqual("orders-acme");
    [Fact] void should_not_hash_when_only_lower_casing() => _acmeUpperCase.ShouldEqual("orders-acme");
    [Fact] void should_treat_default_case_sensitively() => _lowerCaseDefault.ShouldEqual("orders-default");
    [Fact] void should_prepend_the_prefix() => _prefixedDefault.ShouldEqual("cratis-orders");
    [Fact] void should_prepend_the_prefix_before_the_namespace() => _prefixedAcme.ShouldEqual("cratis-orders-acme");
    [Fact] void should_hash_a_name_with_a_space() => _space.ShouldEqual("orders-acme-corp-fd6ee09c");
    [Fact] void should_hash_a_name_with_an_umlaut() => _umlaut.ShouldEqual("orders-m-ller-21f1b1e6");
    [Fact] void should_hash_a_name_with_a_dot() => _dot.ShouldEqual("orders-acme-eu-550a6c3d");
    [Fact] void should_prefix_a_name_not_starting_with_a_letter_and_hash_the_changed_name() => _underscoreStore.ShouldEqual("g--orders-5cfcdfc7");
    [Fact] void should_prefix_a_name_starting_with_a_digit_without_hashing() => _digitStore.ShouldEqual("g-1orders");
    [Fact] void should_hash_a_name_that_is_too_short() => _tooShort.ShouldEqual("ab-fb8e20fc");
    [Fact] void should_keep_a_name_of_exactly_63_characters() => _atTheLimit.ShouldEqual("orders-" + new string('x', 56));
    [Fact] void should_truncate_and_hash_a_name_over_63_characters() => _overTheLimit.ShouldEqual("orders-" + new string('x', 47) + "-1e73c0a9");
    [Fact] void should_replace_each_utf16_char_of_an_emoji() => _emoji.StartsWith("orders--", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_treat_an_empty_namespace_as_none() => _emptyNamespace.ShouldEqual("orders");
    [Fact] void should_treat_a_null_namespace_as_none() => _nullNamespace.ShouldEqual("orders");
}
