// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_SecretNames.when_naming_keys;

public class and_the_identifier_is_overlong : given.key_names
{
    SecretLocations _locations;
    string _revisionName = string.Empty;
    string _erasureName = string.Empty;

    void Because()
    {
        _locations = SecretLocations.For("store", "default", new string('x', 10_000));
        _revisionName = _locations.Name(uint.MaxValue);
        _erasureName = _locations.ErasureName;
    }

    [Fact] void should_keep_the_revision_name_within_the_vault_limit() => _revisionName.Length.ShouldBeLessThanOrEqual(127);
    [Fact] void should_keep_the_erasure_name_within_the_vault_limit() => _erasureName.Length.ShouldBeLessThanOrEqual(127);
    [Fact] void should_only_use_characters_the_vault_accepts_in_the_revision_name() => _revisionName.All(IsAcceptedCharacter).ShouldBeTrue();
    [Fact] void should_only_use_characters_the_vault_accepts_in_the_erasure_name() => _erasureName.All(IsAcceptedCharacter).ShouldBeTrue();

    static bool IsAcceptedCharacter(char character) => character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-';
}
