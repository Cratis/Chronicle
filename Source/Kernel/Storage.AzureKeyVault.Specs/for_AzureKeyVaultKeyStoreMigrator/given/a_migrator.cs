// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator.given;

public class a_migrator : Specification
{
    protected const string Store = "Store";
    protected const string Namespace = "Default";

    protected InMemorySecretVault _vault = default!;
    protected List<LegacyScope> _scopes = default!;
    protected IEncryptionKeyScopeCatalog? _catalog;
    protected EncryptionKeyIdentifier _identifier = (EncryptionKeyIdentifier)"customer-42";

    protected string Legacy(uint revision) => SecretLocations.For(Store, Namespace, _identifier).LegacyName(revision)!;

    protected string Current(uint revision) => SecretLocations.For(Store, Namespace, _identifier).Name(revision);

    protected string CurrentErasure => SecretLocations.For(Store, Namespace, _identifier).ErasureName;

    protected string LegacyErasure => SecretLocations.For(Store, Namespace, _identifier).LegacyErasureName!;

    protected string Key(byte publicByte) => $"{{\"publicKey\":\"{Convert.ToBase64String([publicByte, 1, 2])}\",\"privateKey\":\"{Convert.ToBase64String([9, 3, 4])}\"}}";

    protected string Erasure(uint through, params string[] fingerprints) =>
        $"{{\"erasedThrough\":\"{through}\",\"erasedKeyFingerprints\":\"{string.Join(',', fingerprints)}\",\"newKeyAllowed\":\"False\"}}";

    protected string FingerprintOf(byte publicByte) => new EncryptionKey([publicByte, 1, 2], []).Fingerprint;

    protected Task<EncryptionKeyMigrationReport> Migrate(bool dryRun = false, bool remove = false) =>
        new AzureKeyVaultKeyStoreMigrator(_vault, _scopes, _catalog).Migrate(dryRun, remove);

    void Establish()
    {
        _vault = new InMemorySecretVault();
        _scopes = [new(Store, Namespace)];
    }
}
