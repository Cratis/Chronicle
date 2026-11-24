// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Helpers.for_CustomHttpHeaders;

public class when_adding_valid_headers : Specification
{
    readonly HttpRequestMessage _request = new();

    void Because()
    {
        CustomHttpHeaders.Add(_request.Headers, "X-Custom", "synthetic-value");
        CustomHttpHeaders.Add(_request.Headers, "X-Empty", string.Empty);
        CustomHttpHeaders.Add(_request.Headers, "X-Whitespace", " ");
        CustomHttpHeaders.Add(_request.Headers, "Date", "Tue, 15 Nov 1994 08:12:31 GMT");
    }

    [Fact] void should_preserve_custom_values() => _request.Headers.GetValues("X-Custom").Single().ShouldEqual("synthetic-value");
    [Fact] void should_preserve_empty_values() => _request.Headers.NonValidated["X-Empty"].Single().ShouldEqual(string.Empty);
    [Fact] void should_preserve_whitespace_values() => _request.Headers.NonValidated["X-Whitespace"].Single().ShouldEqual(" ");
    [Fact] void should_parse_standard_headers() => _request.Headers.Date.ShouldEqual(new DateTimeOffset(1994, 11, 15, 8, 12, 31, TimeSpan.Zero));

    void Destroy() => _request.Dispose();
}
