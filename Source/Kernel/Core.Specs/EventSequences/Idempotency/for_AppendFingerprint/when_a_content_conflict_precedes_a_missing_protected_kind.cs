// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_a_content_conflict_precedes_a_missing_protected_kind : given.a_stored_protected_intent
{
    void Establish()
    {
        var later = _original.ProtectedParts[0] with { Kind = ProtectionKind.EncryptedNamespace };
        _original = _original with { ProtectedParts = [.. _original.ProtectedParts, later] };
        _input.Content["name"] = "Grace";
        _input = _input with { ProtectedValues = [new("name", ProtectionKind.Pii, "subject", JsonValue.Create("Grace"))] };
    }

    async Task Because() => _comparison = await Compare();

    [Fact] void should_preserve_the_earlier_content_conflict() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.Conflict);
    [Fact] void should_preserve_the_conflict_reason() => _comparison.Reason.ShouldEqual(FingerprintConflictReason.Content);
}
