// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency.for_StoredAppendFingerprint.when_parsing_the_stored_form;

public class and_it_is_malformed : Specification
{
    FingerprintParseError[] _errors;
    void Because()
    {
        var prefix = $"v1:{new string('a', 64)};type=5:event|1:1|";
        var part = $";pii:{new string('b', 64)}@1={new string('c', 64)}";
        string[] malformed = ["", "v1:", "vx:garbage", $"v1:{new string('a', 64)};type=1:x", prefix[..^1], prefix + ";", prefix + part + part,
            prefix.Replace("5:event", "999999999999999:event", StringComparison.Ordinal), prefix + part.Replace("@1=", "@0=", StringComparison.Ordinal),
            prefix + part.Replace("@1=", "@4294967295=", StringComparison.Ordinal), prefix + part.Replace("@1=", "@01=", StringComparison.Ordinal),
            prefix + part.Replace("pii:", "unknown:", StringComparison.Ordinal), prefix + part[..^1], prefix.ToUpperInvariant(),
            prefix + part.Replace("pii:", "encrypted-global:", StringComparison.Ordinal) + part];
        _errors = malformed.Select(text =>
        {
            StoredAppendFingerprint.Parse(text).TryGetError(out var error).ShouldBeTrue();
            return error;
        }).ToArray();
    }
    [Fact] void should_reject_every_damaged_form() => _errors.ShouldEachConformTo(error => error == FingerprintParseError.MalformedFingerprint);
}
