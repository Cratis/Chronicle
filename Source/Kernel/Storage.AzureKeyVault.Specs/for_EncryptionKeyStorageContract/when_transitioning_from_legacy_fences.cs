// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract;

public class when_transitioning_from_legacy_fences : given.a_fenced_store
{
    [Fact]
    public async Task should_reopen_an_imported_closure_when_the_database_authorizes()
    {
        await CloseLegacy();
        (await Fence())!.NewKeyAllowed.ShouldBeFalse();
        await Authorize();
        (await Fence())!.NewKeyAllowed.ShouldBeTrue();
        (await Storage.GetOrAddWithRevisionFor(EventStore, Namespace, Identifier, Successor)).Key.Private.ShouldEqual(Successor.Private);
    }

    [Fact]
    public async Task should_import_a_new_legacy_closure_after_authorization()
    {
        await CloseLegacy();
        await Authorize();
        var reopened = (await Fence())!;
        await CloseLegacy();
        var imported = (await Fence())!;
        imported.NewKeyAllowed.ShouldBeFalse();
        imported.Lifecycle.ShouldEqual(reopened.Lifecycle);
        imported.Version.ShouldEqual(reopened.Version + 1);
        (await Fence())!.Version.ShouldEqual(imported.Version);
    }

    [Fact]
    public async Task should_tolerate_and_log_a_failed_legacy_authorization_write()
    {
        await CloseLegacy();
        _ = await Fence();
        Client.FailLegacyWrites = true;
        await Authorize();
        (await Fence())!.NewKeyAllowed.ShouldBeTrue();
        Logged("database remains authoritative").ShouldBeTrue();
    }

    [Fact]
    public async Task should_tolerate_and_log_a_failed_legacy_erasure_write()
    {
        Client.FailLegacyWrites = true;
        await Storage.RecordErasureFor(EventStore, Namespace, Identifier, new("erase"));
        (await Fence())!.NewKeyAllowed.ShouldBeFalse();
        Logged("database remains authoritative").ShouldBeTrue();
    }

    [Fact]
    public async Task should_report_no_guarantee_and_warn_for_an_in_memory_authority()
    {
        Authority.IsDurable = false;
        var storage = new AzureKeyVaultEncryptionKeyStorage(Client, Logger, Authority);
        storage.FenceGuarantees.ShouldEqual(EncryptionKeyFenceGuarantees.None);
        Logged("no durable database fence authority").ShouldBeTrue();
        (await storage.GetOrAddFor(EventStore, Namespace, Identifier, Original)).Private.ShouldEqual(Original.Private);
    }
}
