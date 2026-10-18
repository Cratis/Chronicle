// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_only_tags_differ : given.an_append_intent
{
    /// <summary>
    /// Attempt metadata is intentionally absent from AppendFingerprintInput; both attempts reduce to the same intent.
    /// </summary>
    void Because() => _result = Compute(_input with { });
    [Fact] void should_preserve_the_intent() => _result.Format().ShouldEqual(_original.Format());
}
