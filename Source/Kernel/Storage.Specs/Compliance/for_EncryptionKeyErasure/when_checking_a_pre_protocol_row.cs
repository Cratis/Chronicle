// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyErasure;

public class when_checking_a_pre_protocol_row : Specification
{
    readonly EncryptionKey _key = new([1], [2]);
    bool _covered;

    void Because() => _covered = new EncryptionKeyErasure(1u, [], false, ErasedLifecycle: 99).Covers(new(_key, 2u));

    [Fact] void should_not_apply_a_lifecycle_boundary_to_a_row_without_one() => _covered.ShouldBeFalse();
}
