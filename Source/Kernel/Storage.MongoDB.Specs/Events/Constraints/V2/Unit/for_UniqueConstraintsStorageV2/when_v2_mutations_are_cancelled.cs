// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2.Unit.for_UniqueConstraintsStorageV2;

public class when_v2_mutations_are_cancelled : given.a_serverless_v2_storage
{
    [Fact]
    public async Task should_cancel_every_mutation_before_io()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var token = source.Token;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.Save("owner", _definition, 10, _value, _scope, _authority, cancellationToken: token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.Remove("owner", _definition, _scope, _authority, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.RemoveValue("owner", _definition, _value, _scope, _authority, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.ClearValues(_definition, _scope, _authority, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.RemoveAllFor(_definition, _authority, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.RemoveForSubject(_definition, "subject", _authority, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.InitializeAuthority("constraint", _authority, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.TryTransition("constraint", _authority, _authority.Phase, token));
        _database.ReceivedCalls().ShouldBeEmpty();
    }
}
