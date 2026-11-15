// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyErasure;

public class when_validating_a_staged_key_after_erasure : Specification
{
    readonly EncryptionKey _key = new([1], [2]);
    EncryptionKeyErasure _fence;
    Exception _exception;

    void Establish() => _fence = new(1u, [], true, Lifecycle: 3, ErasedLifecycle: 2);

    void Because() => _exception = Catch.Exception(() => _fence.EnsureStillAllowed("subject", new(_key, 99u, 2)));

    [Fact] void should_refuse_a_stage_from_an_erased_lifecycle_even_after_authorization() => _exception.ShouldBeOfExactType<EncryptionKeyErased>();
}
