// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_IdentifierHasher.when_hashing;

public class a_known_value : Specification
{
    string _result;

    void Because() => _result = new IdentifierHasher("chronicle-identifier-hash-key-for-specs-0001").Hash("customer-42");

    [Fact] void should_return_the_truncated_lowercase_hmac() => _result.ShouldEqual("8ce3c6dbf2036756ec5ef13d72ec10c6");
}
