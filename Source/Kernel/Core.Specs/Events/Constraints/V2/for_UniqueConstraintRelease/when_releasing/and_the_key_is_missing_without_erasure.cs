// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_the_key_is_missing_without_erasure : given.a_real_release
{
    Exception _error;

    async Task Establish()
    {
        await StoreProtectedName("Alice");
        await DeleteKeyWithoutErasure();
    }

    async Task Because() => _error = await Catch.Exception(async () => _result = await Create());

    [Fact] void should_throw_missing_encryption_key() => _error.ShouldBeOfExactType<MissingEncryptionKey>();
    [Fact] void should_not_return_a_result() => _result.ShouldBeNull();
}
