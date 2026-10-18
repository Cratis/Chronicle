// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency.for_StoredAppendFingerprint.when_parsing_the_stored_form;

public class and_the_version_is_unknown : Specification
{
    FingerprintParseError _error;
    void Because() => StoredAppendFingerprint.Parse("v2:any future format").TryGetError(out _error).ShouldBeTrue();
    [Fact] void should_report_the_unknown_version() => _error.ShouldEqual(FingerprintParseError.UnknownFingerprintVersion);
}
