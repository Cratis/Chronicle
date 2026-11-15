// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator.given;

public class a_migrator : Specification
{
    protected const string Address = "store/default/customer-42";
    protected const string Current = "k1/store/default/customer-42";

    protected InMemoryVaultSecretStore _store = default!;
    protected IEncryptionKeyStoreMigrator _migrator = default!;

    void Establish()
    {
        _store = new InMemoryVaultSecretStore();
        _migrator = new VaultKeyStoreMigrator(_store, NullLogger<VaultKeyStoreMigrator>.Instance);
    }

    protected static Dictionary<string, string> Key(byte publicByte, byte privateByte = 9) => new()
    {
        ["publicKey"] = Convert.ToBase64String([publicByte, 1, 2]),
        ["privateKey"] = Convert.ToBase64String([privateByte, 3, 4])
    };

    protected static string FingerprintOf(byte publicByte) => new EncryptionKey([publicByte, 1, 2], []).Fingerprint;

    protected static Dictionary<string, string> WithLayout(Dictionary<string, string> data) => new(data) { ["layout"] = "k1" };

    protected Task<EncryptionKeyMigrationReport> Migrate(bool dryRun = false, bool remove = false) => _migrator.Migrate(dryRun, remove);
}
