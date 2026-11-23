// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_invalid_reports_accompany_divergent_keys : given.an_append_intent
{
    FingerprintFailure _divergent;
    FingerprintFailure _identifier;
    FingerprintFailure _kind;
    FingerprintFailure _case;
    string _content;

    void Establish()
    {
        _content = _input.Content.ToJsonString();
        _input = _input with
        {
            ProtectedValues =
            [
                new("name", ProtectionKind.Pii, "subject", _input.Content["name"]!.DeepClone()),
                new("count", ProtectionKind.Pii, "subject", _input.Content["count"]!.DeepClone())
            ]
        };
    }

    void Because()
    {
        var first = Use("name", ProtectionKind.Pii, _key);
        var second = Use("count", ProtectionKind.Pii, new([7, 8, 9], [10, 11, 12]));
        _divergent = Refused(first, second);
        _identifier = Refused(first, second with { Key = second.Key with { Identifier = "other" } });
        _kind = Refused(first, second with { Kind = ProtectionKind.EncryptedSubject });
        _case = Refused(first, second with { Path = "Count" });
    }

    [Fact] void should_identify_divergence_when_reports_are_valid() => _divergent.ShouldEqual(FingerprintFailure.FingerprintKeysDiverged);
    [Fact] void should_refuse_an_invalid_identifier_before_key_divergence() => _identifier.ShouldEqual(FingerprintFailure.FingerprintUnsupported);
    [Fact] void should_refuse_an_invalid_kind_before_key_divergence() => _kind.ShouldEqual(FingerprintFailure.FingerprintUnsupported);
    [Fact] void should_compare_paths_ordinally_before_key_divergence() => _case.ShouldEqual(FingerprintFailure.FingerprintUnsupported);
    [Fact] void should_not_change_content_when_refusing_reports() => _input.Content.ToJsonString().ShouldEqual(_content);

    FingerprintFailure Refused(params ProtectedValueUse[] uses)
    {
        AppendFingerprint.Compute(_input, uses).TryGetError(out var failure).ShouldBeTrue();
        return failure;
    }
}
