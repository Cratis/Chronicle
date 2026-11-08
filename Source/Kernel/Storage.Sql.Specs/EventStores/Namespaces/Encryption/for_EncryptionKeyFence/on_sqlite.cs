// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyFence;

[PerFactSpecificationLifecycle]
public class on_sqlite() : for_EncryptionKeyStorage.given.a_key_store(for_EncryptionKeyStorage.given.a_key_store.Sqlite())
{
    [Fact]
    public async Task should_condition_exact_version_publication_on_the_observed_state()
    {
        IEncryptionKeyFence authority = _harness.Storage;
        var initial = await authority.ReadFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "external");
        var publication = new EncryptionKeyPublication(1, "name", "exact-version", initial.Erasure.Lifecycle, "fingerprint");
        (await authority.PublishRevision(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "external", initial, publication)).ShouldBeTrue();
        (await authority.PublishRevision(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "external", initial, publication)).ShouldBeFalse();
        var read = await authority.ReadFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "external");
        read.Publications.Single().Version.ShouldEqual("exact-version");
        read.Erasure.Version.ShouldEqual(initial.Erasure.Version + 1);
    }

    [Fact]
    public async Task should_keep_external_state_separate_from_native_key_fences()
    {
        IEncryptionKeyFence authority = _harness.Storage;
        var initial = await authority.ReadFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "subject");
        await _harness.Storage.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "subject", new("native"));
        var read = await authority.ReadFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "subject");
        read.Erasure.Version.ShouldEqual(initial.Erasure.Version);
        read.Erasure.NewKeyAllowed.ShouldBeTrue();
    }
}
