// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_CertificatePin;

public class when_parsing_an_invalid_pin : Specification
{
    Exception _missingPrefix;
    Exception _wrongLength;

    void Because()
    {
        _missingPrefix = Catch.Exception(() => CertificatePin.Parse("AAAA"));
        _wrongLength = Catch.Exception(() => CertificatePin.Parse("sha256:AAAA"));
    }

    [Fact] void should_reject_a_pin_without_the_prefix() => _missingPrefix.ShouldBeOfExactType<FormatException>();
    [Fact] void should_reject_a_pin_that_is_not_a_sha256_hash() => _wrongLength.ShouldBeOfExactType<FormatException>();
}
