// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_naming_an_internal_secret;

public class and_event_store_names_would_sanitize_alike : Specification
{
    static readonly string[] _stores = ["sales_eu", "sales-eu", "SALES-EU", "A_B", "a-b", "A-B", "orders", "orders ", "é", "e\u0301", new('x', 2000), new string('x', 2000) + "y"];
    string[] _names;
    string _repeated;

    void Because()
    {
        _names = _stores.Select(store => AzureKeyVaultEncryptionKeyStorage.BuildInternalSecretName(store, InternalSecretPurpose.MutationReceiptDigest)).ToArray();
        _repeated = AzureKeyVaultEncryptionKeyStorage.BuildInternalSecretName(_stores[0], InternalSecretPurpose.MutationReceiptDigest);
    }

    [Fact] void should_preserve_every_exact_store_identity() => _names.Distinct().Count().ShouldEqual(_stores.Length);
    [Fact] void should_be_deterministic() => _repeated.ShouldEqual(_names[0]);
    [Fact] void should_fit_the_vault_name_limit() => _names.All(name => name.Length <= 127).ShouldBeTrue();
    [Fact] void should_use_only_vault_name_characters() => _names.All(name => name.All(character => char.IsAsciiLetterOrDigit(character) || character == '-')).ShouldBeTrue();
    [Fact] void should_stay_in_the_internal_domain() => _names.All(name => name.StartsWith("chronicle-internal--", StringComparison.Ordinal)).ShouldBeTrue();
}
