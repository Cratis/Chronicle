// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

namespace Cratis.Chronicle.Storage.Vault.for_VaultSecretStore;

public class when_paths_contain_transport_delimiters : Specification
{
    const string Path = "store?/default#/customer%2F/1";
    const string TransportPath = "store%3F/default%23/customer%252F/1";
    IKeyValueSecretsEngineV2 _engine;
    VaultSecretStore _store;
    IReadOnlyList<string> _listed;
    VaultSecret? _read;
    int? _written;
    bool _tombstone;
    bool _removed;

    void Establish()
    {
        var client = Substitute.For<IVaultClient>();
        _engine = client.V1.Secrets.KeyValue.V2;
        _engine.ReadSecretPathsAsync(TransportPath, mountPoint: "secret")
            .Returns(new Secret<ListInfo> { Data = new ListInfo { Keys = ["?", "#", "%"] } });
        _engine.ReadSecretAsync(TransportPath, mountPoint: "secret")
            .Returns(new Secret<SecretData>
            {
                Data = new SecretData
                {
                    Data = new Dictionary<string, object> { ["key"] = "value" },
                    Metadata = new CurrentSecretMetadata { Version = 1 }
                }
            });
        _engine.WriteSecretAsync(TransportPath, Arg.Any<IDictionary<string, object>>(), checkAndSet: 0, mountPoint: "secret")
            .Returns(new Secret<CurrentSecretMetadata> { Data = new CurrentSecretMetadata { Version = 1 } });
        _engine.ReadSecretMetadataAsync(TransportPath, mountPoint: "secret")
            .Returns(new Secret<FullSecretMetadata>
            {
                Data = new FullSecretMetadata
                {
                    CurrentVersion = 1,
                    Versions = new Dictionary<string, SecretMetadata> { ["1"] = new() }
                }
            });
        _engine.ReadSecretAsync(TransportPath, version: 1, mountPoint: "secret")
            .Returns(Task.FromException<Secret<SecretData>>(new VaultApiException(HttpStatusCode.NotFound, "version deleted")));
        _store = new VaultSecretStore(client);
    }

    async Task Because()
    {
        _listed = await _store.List(Path);
        _read = await _store.Read(Path);
        _written = await _store.WriteIfAbsent(Path, new Dictionary<string, string> { ["key"] = "value" });
        _tombstone = await _store.IsTombstone(Path);
        _removed = await _store.RemoveVersionIfUnchanged(Path, new VaultSecret(1, new Dictionary<string, string> { ["key"] = "value" }), () => Task.FromResult(true));
        await _store.Remove(Path);
    }

    [Fact] void should_preserve_listed_delimiters() => _listed.ShouldEqual("?", "#", "%");
    [Fact] void should_read_the_literal_path() => _read!.Data["key"].ShouldEqual("value");
    [Fact] void should_write_the_literal_path() => _written.ShouldEqual(1);
    [Fact] void should_read_metadata_and_specific_versions_at_the_literal_path() => _tombstone.ShouldBeTrue();
    [Fact] void should_remove_the_matching_version() => _removed.ShouldBeTrue();
    [Fact] async Task should_escape_version_deletion() => await _engine.Received(1).DeleteSecretVersionsAsync(TransportPath, Arg.Is<IList<int>>(versions => versions.SequenceEqual(new[] { 1 })), mountPoint: "secret");
    [Fact] async Task should_escape_metadata_deletion() => await _engine.Received(1).DeleteMetadataAsync(TransportPath, mountPoint: "secret");
}
