// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_the_content_carries_pii : given.an_append_intent
{
    void Establish() => Protect();
    void Because() => _result = Compute(_input, Use("name", ProtectionKind.Pii, _key));
    [Fact] void should_not_store_the_plaintext() => _result.Format().Contains("Alice", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_store_a_digest_of_the_guessed_plaintext() => _result.Format().Contains(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("Alice"))), StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_store_a_digest_of_the_canonical_guessed_plaintext() => _result.Format().Contains(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("\"Alice\""))), StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_mutate_the_content() => _input.Content["name"]!.GetValue<string>().ShouldEqual("Alice");
}
