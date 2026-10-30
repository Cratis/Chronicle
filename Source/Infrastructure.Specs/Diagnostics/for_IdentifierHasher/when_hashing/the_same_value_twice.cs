// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_IdentifierHasher.when_hashing;

public class the_same_value_twice : Specification
{
    string _first;
    string _second;

    void Because()
    {
        var hasher = new IdentifierHasher("chronicle-identifier-hash-key-for-specs-0001");
        _first = hasher.Hash("customer-42");
        _second = hasher.Hash("customer-42");
    }

    [Fact] void should_return_the_same_hash() => _second.ShouldEqual(_first);
}
