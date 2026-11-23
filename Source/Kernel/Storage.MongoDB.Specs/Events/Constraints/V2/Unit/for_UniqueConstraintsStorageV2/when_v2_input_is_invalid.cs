// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2.Unit.for_UniqueConstraintsStorageV2;

public class when_v2_input_is_invalid : given.a_serverless_v2_storage
{
    [Fact]
    public async Task should_reject_invalid_tokens_and_unrepresentable_owners_before_io()
    {
        await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.Save("owner", _definition, 10, "plaintext", _scope, _authority));
        await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.Save("\ud800", _definition, 10, _value, _scope, _authority));
        await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.Save("owner", _definition, 10, _value, null!, _authority));
        _database.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task should_reject_null_definitions_subjects_and_authority_before_io()
    {
        await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.RemoveAllFor(null!, _authority));
        await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.RemoveForSubject(_definition, null!, _authority));
        await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.Remove("owner", _definition, _scope, null!));
        await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.TryTransition("constraint", _authority, null!));
        _database.ReceivedCalls().ShouldBeEmpty();
    }
}
