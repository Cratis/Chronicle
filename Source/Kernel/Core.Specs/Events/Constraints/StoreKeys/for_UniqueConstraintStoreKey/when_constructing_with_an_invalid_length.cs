// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintStoreKey;

public class when_constructing_with_an_invalid_length : Specification
{
    Exception?[] _errors;

    void Because() => _errors = new[] { 0, 1, 16, 31, 33, 64 }
        .Select(length => Catch.Exception(() => _ = new UniqueConstraintStoreKey(new byte[length])))
        .ToArray();

    [Fact] void should_reject_every_non_256_bit_input() => _errors.All(error => error is InvalidUniqueConstraintStoreKeyLength).ShouldBeTrue();
    [Fact] void should_not_include_material_in_the_error() => _errors.All(error => error!.Message == "Unique-constraint store key material must be exactly 32 bytes.").ShouldBeTrue();
}
