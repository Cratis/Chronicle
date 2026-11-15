// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_SecretNames.when_naming_keys;

public class and_components_are_shifted_between_positions : given.key_names
{
    bool _firstShiftDiffers;
    bool _secondShiftDiffers;

    void Because()
    {
        _firstShiftDiffers = Differ(["ab", "c", "d"], ["a", "bc", "d"]);
        _secondShiftDiffers = Differ(["a", "b", "cd"], ["a", "bc", "d"]);
    }

    [Fact] void should_tell_a_character_moved_from_the_event_store_to_the_namespace_apart() => _firstShiftDiffers.ShouldBeTrue();
    [Fact] void should_tell_a_character_moved_from_the_namespace_to_the_identifier_apart() => _secondShiftDiffers.ShouldBeTrue();
}
