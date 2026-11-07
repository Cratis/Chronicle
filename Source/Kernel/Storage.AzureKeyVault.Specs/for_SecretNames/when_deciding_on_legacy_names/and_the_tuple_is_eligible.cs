// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_SecretNames.when_deciding_on_legacy_names;

public class and_the_tuple_is_eligible : given.key_names
{
    List<string> _ineligible = [];

    void Because()
    {
        (string Name, string? Legacy)[] results =
        [
            ("guid", LegacyOf("store-1", "Default", SampleGuid)),
            ("canonical", LegacyOf("store", "default", "customer-42")),
            ("global", LegacyOf(EncryptedValueKeyIdentifiers.GlobalEventStore, EncryptedValueKeyIdentifiers.GlobalNamespace, EncryptedValueKeyIdentifiers.ForGlobal())),
            ("namespace", LegacyOf("store", "Default", EncryptedValueKeyIdentifiers.ForNamespace())),
            ("subject", LegacyOf("store", "Default", EncryptedValueKeyIdentifiers.ForSubject(SampleGuid)))
        ];
        _ineligible = [.. results.Where(result => result.Legacy is null).Select(result => result.Name)];
    }

    [Fact] void should_offer_an_earlier_name_for_each() => _ineligible.ShouldBeEmpty();
    [Fact] void should_use_the_lowercased_sanitized_components() => LegacyOf("store-1", "Default", SampleGuid).ShouldEqual($"chronicle--store-1--default--{SampleGuid}--");
}
