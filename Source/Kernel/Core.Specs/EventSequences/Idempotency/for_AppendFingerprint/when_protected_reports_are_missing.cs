// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_protected_reports_are_missing : given.an_append_intent
{
    FingerprintFailure _failure;
    void Establish() => Protect();
    void Because() => AppendFingerprint.Compute(_input, []).TryGetError(out _failure).ShouldBeTrue();
    [Fact] void should_not_silently_drop_protected_content() => _failure.ShouldEqual(FingerprintFailure.FingerprintUnsupported);
}
