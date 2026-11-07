// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Vault.for_VaultPathSegments.when_deciding_on_legacy_directories;

public class and_the_address_is_eligible : given.segments
{
    string? _norwegian;
    string? _spanish;
    string? _ordinary;
    string? _tilde;
    string? _punctuation;

    void Because()
    {
        _norwegian = VaultPathSegments.LegacyDirectory("store", "default", "\u00D8yvind");
        _spanish = VaultPathSegments.LegacyDirectory("store", "default", "Jos\u00E9");
        _ordinary = VaultPathSegments.LegacyDirectory("store", "default", "customer-42");
        _tilde = VaultPathSegments.LegacyDirectory("store", "default", "a~7E");
        _punctuation = VaultPathSegments.LegacyDirectory("store", "default", "a$b@c.d");
    }

    [Fact] void should_use_the_raw_directory_for_a_letter_outside_ascii() => _norwegian.ShouldEqual("store/default/\u00D8yvind");
    [Fact] void should_use_the_raw_directory_for_an_accented_letter() => _spanish.ShouldEqual("store/default/Jos\u00E9");
    [Fact] void should_use_the_raw_directory_for_ordinary_text() => _ordinary.ShouldEqual("store/default/customer-42");
    [Fact] void should_use_the_raw_directory_for_text_with_a_tilde() => _tilde.ShouldEqual("store/default/a~7E");
    [Fact] void should_use_the_raw_directory_for_punctuation_kept_as_it_is() => _punctuation.ShouldEqual("store/default/a$b@c.d");
}
