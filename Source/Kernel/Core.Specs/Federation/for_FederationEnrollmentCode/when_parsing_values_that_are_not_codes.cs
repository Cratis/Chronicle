// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentCode;

public class when_parsing_values_that_are_not_codes : Specification
{
    bool _null;
    bool _empty;
    bool _wrongPrefix;
    bool _notBase64;
    bool _notJson;
    bool _plainHttp;
    bool _missingSecret;

    void Because()
    {
        _null = FederationEnrollmentCode.TryParse(null, out _);
        _empty = FederationEnrollmentCode.TryParse(string.Empty, out _);
        _wrongPrefix = FederationEnrollmentCode.TryParse("nope.abcd", out _);
        _notBase64 = FederationEnrollmentCode.TryParse("cfe1.!!!", out _);
        _notJson = FederationEnrollmentCode.TryParse("cfe1.aGVsbG8", out _);
        _plainHttp = FederationEnrollmentCode.TryParse(new FederationEnrollmentCode("http://federation.example.com", "ab", "id", "secret").Format(), out _);
        _missingSecret = FederationEnrollmentCode.TryParse(new FederationEnrollmentCode("https://federation.example.com", "ab", "id", string.Empty).Format(), out _);
    }

    [Fact] void should_not_parse_null() => _null.ShouldBeFalse();
    [Fact] void should_not_parse_empty() => _empty.ShouldBeFalse();
    [Fact] void should_not_parse_another_prefix() => _wrongPrefix.ShouldBeFalse();
    [Fact] void should_not_parse_what_is_not_base64() => _notBase64.ShouldBeFalse();
    [Fact] void should_not_parse_what_is_not_json() => _notJson.ShouldBeFalse();
    [Fact] void should_not_parse_an_endpoint_that_is_not_https() => _plainHttp.ShouldBeFalse();
    [Fact] void should_not_parse_a_code_without_a_secret() => _missingSecret.ShouldBeFalse();
}
