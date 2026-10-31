// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Types;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Chronicle.Benchmarks.Observer;

/// <summary>
/// Measures the kernel's PII schema traversal with real encryption and substituted key storage.
/// </summary>
/// <remarks>
/// Schema metadata is the representation produced for [PII] properties, including a leaf in a referenced nested type.
/// Schema parsing, key generation, and ciphertext preparation are outside measurement. Allocations include the
/// kernel manager, encryption, and key-storage substitutes; client schema generation and external storage are excluded.
/// </remarks>
[MemoryDiagnoser]
public class SchemaMetadataPaths
{
    JsonSchemaMetadataManager _manager = null!;
    JsonSchema _schema = null!;
    JsonObject _plaintext = null!;
    JsonObject _protected = null!;
    IEncryptionKeyStorage _keys = null!;
    IManagedEncryptionKeyProvisioner _provisioner = null!;

    /// <summary>
    /// Prepares the nested PII schema and verifies that both protected leaves round-trip.
    /// </summary>
    /// <returns>The setup task.</returns>
    /// <exception cref="InvalidOperationException">The schema or protection path was not exercised.</exception>
    [GlobalSetup]
    public async Task Setup()
    {
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "name": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] },
                "contact": { "$ref": "#/definitions/Contact" },
                "note": { "type": "string" }
              },
              "definitions": {
                "Contact": {
                  "type": "object",
                  "properties": {
                    "email": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] },
                    "city": { "type": "string" }
                  }
                }
              }
            }
            """);
        _plaintext = JsonNode.Parse("""{"name":"Benchmark Person","contact":{"email":"person@example.test","city":"Oslo"},"note":"public"}""")!.AsObject();
        var encryption = new Encryption();
        var key = encryption.GenerateKey();
        _keys = Substitute.For<IEncryptionKeyStorage>();
        _keys.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).Returns(key);
        _provisioner = Substitute.For<IManagedEncryptionKeyProvisioner>();
        _provisioner.EnsureKeyWithRevisionFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>())
            .Returns(new EncryptionKeyAtRevision(key, EncryptionKeyRevision.Initial));
        var handler = new PIICompliancePropertyValueHandler(_provisioner, _keys, encryption);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
        _protected = await ApplyPii();
        var released = await ReleasePii();
        if (_schema.Properties["contact"].Reference is null ||
            JsonNode.DeepEquals(_protected["name"], _plaintext["name"]) ||
            JsonNode.DeepEquals(_protected["contact"]!["email"], _plaintext["contact"]!["email"]) ||
            !JsonNode.DeepEquals(released, _plaintext))
        {
            throw new InvalidOperationException("Nested PII protection did not round-trip through the referenced schema.");
        }
        ClearCalls();
    }

    /// <summary>
    /// Clears substituted key-storage invocation records outside measurement.
    /// </summary>
    [IterationCleanup]
    public void ClearCalls()
    {
        _keys.ClearReceivedCalls();
        _provisioner.ClearReceivedCalls();
    }

    /// <summary>
    /// Applies PII metadata to the two protected leaves through the real kernel manager.
    /// </summary>
    /// <returns>The protected document.</returns>
    [Benchmark]
    public Task<JsonObject> ApplyPii() => _manager.Apply("benchmark-store", "benchmark-namespace", _schema, "benchmark-subject", _plaintext);

    /// <summary>
    /// Releases a prepared protected document through the real kernel manager.
    /// </summary>
    /// <returns>The released document.</returns>
    [Benchmark]
    public Task<JsonObject> ReleasePii() => _manager.Release("benchmark-store", "benchmark-namespace", _schema, "benchmark-subject", _protected);
}
