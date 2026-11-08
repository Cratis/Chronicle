// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract;

public class when_publication_and_erasure_interleave : given.a_fenced_store
{
    [Fact]
    public async Task should_disable_only_the_losing_version_when_a_successor_shares_the_name()
    {
        Client.BeforeStage = async () =>
        {
            await Storage.RecordErasureFor(EventStore, Namespace, Identifier, new("empty"));
            await Authorize();
            await Storage.SaveFor(EventStore, Namespace, Identifier, Successor, 2u);
        };
        await Assert.ThrowsAsync<EncryptionKeyErased>(() => Storage.SaveFor(EventStore, Namespace, Identifier, Original, 2u));
        (await Storage.GetFor(EventStore, Namespace, Identifier, 2u)).Private.ShouldEqual(Successor.Private);
        Client.Disabled.Count.ShouldEqual(1);
        Client.Purged.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_fail_closed_when_the_authority_is_unavailable_after_staging()
    {
        Client.AfterStage = () =>
        {
            Authority.Unavailable = true;
            return Task.CompletedTask;
        };
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => Storage.SaveFor(EventStore, Namespace, Identifier, Original));
        Authority.Unavailable = false;
        (await Storage.TryGetLatestFor(EventStore, Namespace, Identifier)).ShouldBeNull();
        Client.Purged.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_not_confirm_a_soft_delete_when_purge_is_denied()
    {
        await Storage.SaveFor(EventStore, Namespace, Identifier, Original);
        await Storage.RecordErasureFor(EventStore, Namespace, Identifier, new("erase"));
        Client.FailPurge = true;
        await Assert.ThrowsAsync<EncryptionKeyNotPurged>(() => Storage.DeleteThroughFor(EventStore, Namespace, Identifier, "erase"));
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => Storage.ConfirmErasureFor(EventStore, Namespace, Identifier, "erase"));
        (await Fence()).Requests.Single().Confirmed.ShouldBeFalse();
    }

    [Fact]
    public async Task should_never_serve_an_unpublished_later_version_of_a_published_name()
    {
        await Storage.SaveFor(EventStore, Namespace, Identifier, Original, 1u);
        Client.AfterStage = async () => (await Storage.GetFor(EventStore, Namespace, Identifier, 1u)).Private.ShouldEqual(Original.Private);
        await Storage.SaveFor(EventStore, Namespace, Identifier, Successor, 1u);
        (await Storage.GetFor(EventStore, Namespace, Identifier, 1u)).Private.ShouldEqual(Successor.Private);
    }
}
