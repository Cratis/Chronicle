// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentCode;

public class when_formatting_and_parsing : Specification
{
    FederationEnrollmentCode _original;
    string _formatted;
    bool _parsed;
    FederationEnrollmentCode _roundTripped;

    void Establish() => _original = new("https://federation.example.com:35100/", "abcdef0123", "enrollment-1", "s3cr3t-value");

    void Because()
    {
        _formatted = _original.Format();
        _parsed = FederationEnrollmentCode.TryParse(_formatted, out _roundTripped);
    }

    [Fact] void should_be_one_string_without_whitespace() => _formatted.ShouldNotContain(" ");
    [Fact] void should_parse() => _parsed.ShouldBeTrue();
    [Fact] void should_carry_everything_through() => _roundTripped.ShouldEqual(_original);
    [Fact] void should_not_show_the_secret_in_the_clear() => _formatted.ShouldNotContain("s3cr3t-value");
    [Fact] void should_not_show_the_secret_when_printed() => _original.ToString().ShouldNotContain("s3cr3t-value");
}
