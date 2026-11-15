// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_SecretNames.when_naming_keys;

public class and_text_differs_only_in_unicode_form : given.key_names
{
    bool _normalizationDiffers;
    bool _dotlessDiffers;
    bool _fullWidthDiffers;

    void Because()
    {
        _normalizationDiffers = Differ(["store", "default", "\u00E9"], ["store", "default", "e\u0301"]);
        _dotlessDiffers = Differ(["store", "default", "I"], ["store", "default", "\u0131"]);
        _fullWidthDiffers = Differ(["store", "default", "\uFF21"], ["store", "default", "A"]);
    }

    [Fact] void should_tell_composed_and_decomposed_apart() => _normalizationDiffers.ShouldBeTrue();
    [Fact] void should_tell_a_dotless_i_from_an_upper_case_i() => _dotlessDiffers.ShouldBeTrue();
    [Fact] void should_tell_a_full_width_letter_from_its_ascii_form() => _fullWidthDiffers.ShouldBeTrue();
}
