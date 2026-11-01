// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_IdentifierHasher.when_hashing;

public class into_a_span : Specification
{
    bool _success;
    int _written;
    string _result;

    void Because()
    {
        Span<char> destination = stackalloc char[32];
        _success = new IdentifierHasher("chronicle-identifier-hash-key-for-specs-0001").TryHash("customer-42", destination, out _written);
        _result = destination[.._written].ToString();
    }

    [Fact] void should_succeed() => _success.ShouldBeTrue();
    [Fact] void should_write_32_characters() => _written.ShouldEqual(32);
    [Fact] void should_return_the_same_hash() => _result.ShouldEqual("8ce3c6dbf2036756ec5ef13d72ec10c6");
}
