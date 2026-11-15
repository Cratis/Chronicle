// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintStoreKeyIdBytes;

public class when_converting_an_upper_case_id : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => UniqueConstraintStoreKeyIdBytes.From(new UniqueConstraintStoreKeyId("00FF10A5B6C7D8E9F0123456789ABCDE")));

    [Fact] void should_throw_invalid_key_id() => _error.ShouldBeOfExactType<InvalidUniqueConstraintStoreKeyId>();
    [Fact] void should_not_reveal_the_value() => _error.Message.ShouldNotContain("00FF");
}
