// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_IdentifierHasher.when_hashing;

public class into_a_short_span : Specification
{
    bool _success;
    int _written;

    void Because()
    {
        Span<char> destination = stackalloc char[31];
        _success = new IdentifierHasher("chronicle-identifier-hash-key-for-specs-0001").TryHash("customer-42", destination, out _written);
    }

    [Fact] void should_not_succeed() => _success.ShouldBeFalse();
    [Fact] void should_not_write_any_characters() => _written.ShouldEqual(0);
}
