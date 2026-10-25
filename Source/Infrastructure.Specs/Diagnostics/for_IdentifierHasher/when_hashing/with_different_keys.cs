// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_IdentifierHasher.when_hashing;

public class with_different_keys : Specification
{
    string _first;
    string _second;

    void Because()
    {
        _first = new IdentifierHasher("chronicle-identifier-hash-key-for-specs-0001").Hash("customer-42");
        _second = new IdentifierHasher("chronicle-identifier-hash-key-for-specs-0002").Hash("customer-42");
    }

    [Fact] void should_return_different_hashes() => _second.ShouldNotEqual(_first);
}
