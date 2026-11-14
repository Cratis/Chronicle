// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Diagnostics.Identifiers.for_IdentifierRedactor;

public class when_hashing_identifiers : Specification
{
    string _result;
    int _length;
    int _written;

    void Because()
    {
        var redactor = new IdentifierRedactor(new IdentifierTelemetry(new Telemetry
        {
            EventSourceIdsInLogs = IdentifierExposure.Hashed,
            IdentifierHashKey = "chronicle-identifier-hash-key-for-specs-0001"
        }));
        _length = redactor.GetRedactedLength("customer-42".AsSpan());
        Span<char> destination = stackalloc char[_length];
        _written = redactor.Redact("customer-42".AsSpan(), destination);
        _result = destination[.._written].ToString();
    }

    [Fact] void should_hash_the_identifier() => _result.ShouldEqual("8ce3c6dbf2036756ec5ef13d72ec10c6");
    [Fact] void should_write_32_characters() => _written.ShouldEqual(32);
    [Fact] void should_report_the_written_length() => _length.ShouldEqual(_written);
}
