// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint.when_the_key_changes_during_the_append;

public class and_two_values_of_one_kind_get_different_revisions : given.an_append_intent
{
    FingerprintFailure _failure;
    void Establish()
    {
        Protect();
        _input = _input with { ProtectedValues = [.. _input.ProtectedValues, new("count", ProtectionKind.Pii, "subject", _input.Content["count"]!.DeepClone())] };
    }
    void Because() => AppendFingerprint.Compute(_input, [Use("name", ProtectionKind.Pii, _key), Use("count", ProtectionKind.Pii, _key, 2)]).TryGetError(out _failure).ShouldBeTrue();
    [Fact] void should_refuse_the_unsettled_revisions() => _failure.ShouldEqual(FingerprintFailure.FingerprintKeysDiverged);
}
