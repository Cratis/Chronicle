// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_SecretNames.when_naming_keys;

public class and_the_prefix_is_compared_with_other_names : given.key_names
{
    string _short = string.Empty;
    string _long = string.Empty;
    string _other = string.Empty;

    void Because()
    {
        _short = PrefixOf("s", "n", "i");
        _long = PrefixOf("a-much-longer-event-store-name", "a-much-longer-namespace", new string('i', 500));
        _other = PrefixOf("s", "n", "j");
    }

    [Fact] void should_start_with_the_versioned_prefix() => _short.StartsWith("chronicle-k1--", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_start_like_an_earlier_name() => _short.StartsWith("chronicle--", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_start_like_an_internal_name() => _short.StartsWith("chronicle-internal", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_have_a_fixed_length() => _long.Length.ShouldEqual(_short.Length);
    [Fact] void should_not_be_a_prefix_of_another_tuples_prefix() => _other.StartsWith(_short, StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
}
