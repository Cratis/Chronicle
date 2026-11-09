// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract;

public class when_durability_is_lost_during_release_read : given.a_fenced_store
{
    readonly TaskCompletionSource _entered = new();
    readonly TaskCompletionSource _resume = new();
    Exception _exception;

    async Task Establish()
    {
        await Storage.RecordErasureFor(EventStore, Namespace, Identifier, new("request"));
        Authority.BeforeRead = async () =>
        {
            _entered.SetResult();
            await _resume.Task;
        };
    }

    async Task Because()
    {
        var release = Catch.Exception(() => Storage.GetErasureForRelease(EventStore, Namespace, Identifier));
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Authority.IsDurable = false;
        _resume.SetResult();
        _exception = await release;
    }

    [Fact] void should_not_authorize_release() => _exception.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
}
