// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintEncodingStorage;

public class when_inserting_a_second_key : given.a_key_storage
{
    UniqueConstraintEncodingKey _original;
    UniqueConstraintEncodingKey _stored;
    UniqueConstraintEncodingKey _differentCandidateResult;
    UniqueConstraintEncodingKey _sameCandidateResult;

    async Task Establish()
    {
        _original = UniqueConstraintEncodingKey.Create();
        await _storage.CreateKeyIfAbsent(_original);
    }

    async Task Because()
    {
        _differentCandidateResult = await _storage.CreateKeyIfAbsent(UniqueConstraintEncodingKey.Create());
        _sameCandidateResult = await _storage.CreateKeyIfAbsent(_original);
        _stored = (await _storage.GetKey())!;
    }

    [Fact] void should_return_the_original_for_a_different_candidate() => _differentCandidateResult.ShouldEqual(_original);
    [Fact] void should_return_the_original_for_the_same_candidate() => _sameCandidateResult.ShouldEqual(_original);
    [Fact] void should_preserve_the_original_identity() => _stored.KeyId.ShouldEqual(_original.KeyId);
    [Fact] void should_preserve_the_original_material() => Assert.Equal(_original.Material.CopyBytes(), _stored.Material.CopyBytes());
}
