// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_provisioning_a_key_concurrently;

/// <summary>
/// REPRO: a subject receives many PII values encrypted concurrently (a batch append + sibling
/// projections encrypt every PII field under the same subject at once, via Task.WhenAll). The
/// real key-provisioning path must yield exactly one key and every ciphertext must release back
/// to plaintext. A padding failure on release means the key changed between encrypt and read.
/// </summary>
public class for_the_same_subject : Specification
{
    const string Identifier = "39b34712-ad8e-4cde-b879-2719c995aa49";
    const int Count = 64;

    InMemoryEncryptionKeyStorage _keyStore;
    PIICompliancePropertyValueHandler _handler;
    string[] _plaintexts;
    JsonNode[] _ciphertexts;
    Exception _releaseError;
    string[] _released;
    int _revisionCount;

    void Establish()
    {
        _keyStore = new InMemoryEncryptionKeyStorage();
        _handler = new(_keyStore, new Encryption());
        _plaintexts = Enumerable.Range(0, Count).Select(i => $"secret-{i}").ToArray();
    }

    async Task Because()
    {
        var applyTasks = _plaintexts.Select(p =>
            _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, JsonValue.Create(p)));
        _ciphertexts = await Task.WhenAll(applyTasks);

        _releaseError = await Catch.Exception(async () =>
        {
            var releaseTasks = _ciphertexts.Select(c =>
                _handler.Release(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, c));
            var results = await Task.WhenAll(releaseTasks);
            _released = results.Select(r => r.ToString()).ToArray();
        });

        _revisionCount = await CountRevisions();
    }

    async Task<int> CountRevisions()
    {
        var count = 0;
        for (var revision = 1u; revision <= Count + 1; revision++)
        {
            if (await _keyStore.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, new EncryptionKeyRevision(revision)))
            {
                count++;
            }
        }

        return count;
    }

    [Fact] void should_release_every_value_without_a_padding_failure() => _releaseError.ShouldBeNull();
    [Fact] void should_round_trip_every_value() => _released.ShouldContainOnly(_plaintexts);
    [Fact] void should_provision_exactly_one_key_revision() => _revisionCount.ShouldEqual(1);
}
