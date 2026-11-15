// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintStoreKeyIdBytes;

public class when_converting_a_short_id : Specification
{
    Exception _fromError;
    Exception _toError;

    void Because()
    {
        _fromError = Catch.Exception(() => UniqueConstraintStoreKeyIdBytes.From(new UniqueConstraintStoreKeyId("00ff")));
        _toError = Catch.Exception(() => UniqueConstraintStoreKeyIdBytes.To(new byte[15]));
    }

    [Fact] void should_reject_a_short_id() => _fromError.ShouldBeOfExactType<InvalidUniqueConstraintStoreKeyId>();
    [Fact] void should_reject_short_bytes() => _toError.ShouldBeOfExactType<InvalidUniqueConstraintStoreKeyId>();
}
