// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_provisioning_a_key_concurrently;

/// <summary>
/// REPRO of the residual GDPR-PII defect: a subject owns two read models (a root-PII model A and a
/// sibling child-collection-PII model B) that both encrypt under the same subject. When the second
/// provisioner's existence check reads state that does not yet reflect the first save — a MongoDB
/// secondary lagging the primary, or a second silo — check-then-act provisioning saves a SECOND key.
/// Because a save mints a new revision and reads return the latest, the value model A already encrypted
/// under the first key can no longer be decrypted: "rsa routines::padding check failed". Provisioning
/// must converge to exactly one key pair per subject so the earlier value still releases.
/// </summary>
public class and_a_provisioning_read_is_stale : given.a_key_store_with_read_after_write_lag
{
    readonly string _subject = Guid.NewGuid().ToString();
    const string ModelARootValue = "Jane Doe";
    const string ModelBChildValue = "A sensitive evaluation note";

    PIICompliancePropertyValueHandler _handler;
    JsonNode _modelACiphertext;
    Exception _releaseError;
    string _modelAReleased;
    int _revisionCount;

    void Establish() => _handler = new(_keyStore, new Encryption());

    async Task Because()
    {
        // Model A encrypts its root PII under the subject — provisions the first key.
        _modelACiphertext = await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _subject, JsonValue.Create(ModelARootValue));

        // Model B's projection encrypts a child-collection PII under the same subject, but its existence
        // check still reads the stale secondary and does not see model A's key — so it provisions again.
        await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _subject, JsonValue.Create(ModelBChildValue));

        await _keyStore.Replicate();

        _releaseError = await Catch.Exception(async () =>
        {
            var released = await _handler.Release(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _subject, _modelACiphertext);
            _modelAReleased = released.ToString();
        });

        _revisionCount = await _keyStore.RevisionCountOnPrimary(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _subject);
    }

    [Fact] void should_release_model_a_root_pii_without_a_padding_failure() => _releaseError.ShouldBeNull();
    [Fact] void should_release_model_a_root_pii_back_to_plaintext() => _modelAReleased.ShouldEqual(ModelARootValue);
    [Fact] void should_provision_exactly_one_key_pair_for_the_subject() => _revisionCount.ShouldEqual(1);
}
