// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.ExternalServices.for_ExternalService;

public class when_listing_with_custom_headers : given.stored_headers
{
    ExternalService _result;

    async Task Because() => _result = (await ExternalService.GetExternalServices("store", _storage)).Single();

    [Fact] void should_keep_the_names() => _result.Headers.Keys.ShouldContainOnly("x-first", "x-second", "x-empty", "x-whitespace");
    [Fact] void should_redact_all_legacy_values() => _result.Headers.Values.All(_ => _.Length == 0).ShouldBeTrue();
    [Fact] void should_include_all_presence_flags() => _result.HeaderValuesSet.Keys.ShouldContainOnly(_headers.Keys);
    [Fact] void should_report_the_first_value_as_set() => _result.HeaderValuesSet["x-first"].ShouldBeTrue();
    [Fact] void should_report_the_second_value_as_set() => _result.HeaderValuesSet["x-second"].ShouldBeTrue();
    [Fact] void should_report_an_empty_value_as_unset() => _result.HeaderValuesSet["x-empty"].ShouldBeFalse();
    [Fact] void should_report_whitespace_as_set() => _result.HeaderValuesSet["x-whitespace"].ShouldBeTrue();
    [Fact] void should_exclude_header_values_and_authorization_from_json() => JsonSerializer.Serialize(_result).ShouldNotContain("synthetic-");
    [Fact] void should_leave_stored_values_untouched() => _headers["x-first"].ShouldEqual("synthetic-first");
    [Fact] void should_leave_registration_values_untouched() => _definition.ToContract().Endpoint.Http!.Headers["x-second"].ShouldEqual("synthetic-second");
}
