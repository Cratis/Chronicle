// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_protected_reports_do_not_match_the_boundaries : given.an_append_intent
{
    FingerprintFailure _duplicatePath;
    FingerprintFailure _otherPath;
    FingerprintFailure _otherKeyKind;
    FingerprintFailure _otherPlaintext;
    FingerprintFailure _unusableRevision;
    FingerprintFailure _maximumRevision;
    FingerprintFailure _extraReport;
    bool _matchingSucceeds;

    void Establish()
    {
        _input = _input with { Content = JsonNode.Parse("""{"name":"Alice","count":1}""")!.AsObject() };
        _input = _input with { ProtectedValues = [new("name", ProtectionKind.Pii, "subject", _input.Content["name"]!.DeepClone()), new("count", ProtectionKind.Pii, "subject", _input.Content["count"]!.DeepClone())] };
    }

    void Because()
    {
        var name = Use("name", ProtectionKind.Pii, _key);
        var count = Use("count", ProtectionKind.Pii, _key);
        _matchingSucceeds = AppendFingerprint.Compute(_input, [name, count]).TryGetResult(out _);
        _duplicatePath = Refused(name, name);
        _otherPath = Refused(name, new("other", ProtectionKind.Pii, JsonValue.Create(1), new(ProtectionKind.Pii, "subject", _key, EncryptionKeyRevision.Initial)));
        _otherKeyKind = Refused(name, new("count", ProtectionKind.Pii, JsonValue.Create(1), new(ProtectionKind.EncryptedSubject, "subject", _key, EncryptionKeyRevision.Initial)));
        _otherPlaintext = Refused(name, new("count", ProtectionKind.Pii, JsonValue.Create(2), new(ProtectionKind.Pii, "subject", _key, EncryptionKeyRevision.Initial)));
        _unusableRevision = Refused(name, new("count", ProtectionKind.Pii, JsonValue.Create(1), new(ProtectionKind.Pii, "subject", _key, new EncryptionKeyRevision(0u))));
        _maximumRevision = Refused(name, new("count", ProtectionKind.Pii, JsonValue.Create(1), new(ProtectionKind.Pii, "subject", _key, new EncryptionKeyRevision(uint.MaxValue))));
        _extraReport = Refused(name, count, Use("name", ProtectionKind.Pii, _key));
    }

    [Fact] void should_accept_matching_reports() => _matchingSucceeds.ShouldBeTrue();
    [Fact] void should_refuse_a_duplicate_path() => _duplicatePath.ShouldEqual(FingerprintFailure.FingerprintUnsupported);
    [Fact] void should_refuse_a_report_for_another_path() => _otherPath.ShouldEqual(FingerprintFailure.FingerprintUnsupported);
    [Fact] void should_refuse_a_report_for_another_key_kind() => _otherKeyKind.ShouldEqual(FingerprintFailure.FingerprintUnsupported);
    [Fact] void should_refuse_a_report_for_other_plaintext() => _otherPlaintext.ShouldEqual(FingerprintFailure.FingerprintUnsupported);
    [Fact] void should_refuse_revision_zero() => _unusableRevision.ShouldEqual(FingerprintFailure.FingerprintUnsupported);
    [Fact] void should_refuse_the_maximum_revision() => _maximumRevision.ShouldEqual(FingerprintFailure.FingerprintUnsupported);
    [Fact] void should_refuse_an_extra_report() => _extraReport.ShouldEqual(FingerprintFailure.FingerprintUnsupported);

    FingerprintFailure Refused(params ProtectedValueUse[] uses)
    {
        AppendFingerprint.Compute(_input, uses).TryGetError(out var failure).ShouldBeTrue();
        return failure;
    }
}
