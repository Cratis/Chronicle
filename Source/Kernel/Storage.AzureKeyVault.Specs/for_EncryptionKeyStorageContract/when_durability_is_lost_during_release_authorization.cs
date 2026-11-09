// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract;

public class when_durability_is_lost_during_release_authorization : given.a_fenced_store
{
    readonly TaskCompletionSource _entered = new();
    readonly TaskCompletionSource _resume = new();
    EncryptionKeyErasure _fence;
    Exception _exception;

    async Task Establish()
    {
        await Storage.RecordErasureFor(EventStore, Namespace, Identifier, new("request"));
        await Storage.ConfirmErasureFor(EventStore, Namespace, Identifier, "request");
        _fence = await Fence();
        Authority.AfterExchange = async () =>
        {
            _entered.SetResult();
            await _resume.Task;
        };
    }

    async Task Because()
    {
        var authorization = Catch.Exception(() => Storage.AllowNewKeyFor(EventStore, Namespace, Identifier, _fence.Version, _fence.Lifecycle));
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Authority.IsDurable = false;
        _resume.SetResult();
        _exception = await authorization;
    }

    [Fact] void should_fail_closed_even_when_the_write_applied() => _exception.ShouldBeOfExactType<EncryptionKeyFenceWriteOutcomeUnknown>();
}
