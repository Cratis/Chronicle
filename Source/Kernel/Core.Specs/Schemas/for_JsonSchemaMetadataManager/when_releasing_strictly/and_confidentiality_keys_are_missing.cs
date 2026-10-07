// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_strictly;

public class and_confidentiality_keys_are_missing : Specification
{
    JsonSchemaMetadataManager _manager;
    readonly List<(JsonSchema Schema, JsonObject Content)> _values = [];
    readonly List<Exception> _errors = [];

    async Task Establish()
    {
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(keys, encryption);
        _manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
                new EncryptedSubjectValueHandler(provisioner, keys, encryption),
                new EncryptedNamespaceValueHandler(provisioner, keys, encryption),
                new EncryptedGlobalValueHandler(provisioner, keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        foreach (var metadata in new[] { "EncryptedSubject", "EncryptedNamespace", "EncryptedGlobal" })
        {
            var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"secret":{"type":"string","security":[{"metadataType":"METADATA","details":""}]}}}""".Replace("METADATA", metadata, StringComparison.Ordinal));
            _values.Add((schema, await _manager.Apply("store", "namespace", schema, "owner", new JsonObject { ["secret"] = "secret" })));
        }
        await keys.DeleteFor("store", "namespace", EncryptedValueKeyIdentifiers.ForSubject("owner"));
        await keys.DeleteFor("store", "namespace", EncryptedValueKeyIdentifiers.ForNamespace());
        await keys.DeleteFor(EncryptedValueKeyIdentifiers.GlobalEventStore, EncryptedValueKeyIdentifiers.GlobalNamespace, EncryptedValueKeyIdentifiers.ForGlobal());
    }

    async Task Because()
    {
        foreach (var (schema, content) in _values)
        {
            _errors.Add(await Catch.Exception(() => _manager.ReleaseStrict("store", "namespace", schema, "owner", content)));
        }
    }

    [Fact] void should_fail_for_every_confidentiality_scope() => _errors.TrueForAll(error => error is SchemaMetadataActionFailed).ShouldBeTrue();
    [Fact] void should_not_modify_stored_content() => _values.TrueForAll(value => value.Content["secret"]!.GetValue<string>().Length > 0).ShouldBeTrue();
}
