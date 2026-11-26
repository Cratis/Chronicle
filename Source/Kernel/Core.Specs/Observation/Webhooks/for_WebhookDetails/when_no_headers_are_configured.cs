// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDetails;

public class when_no_headers_are_configured : given.stored_headers
{
    WebhookDetails _result;

    void Establish() => _headers.Clear();

    async Task Because() => _result = (await WebhookDetails.GetWebhooks("store", _storage)).Single();

    [Fact] void should_have_no_header_names() => _result.Headers.ShouldBeEmpty();
    [Fact] void should_have_no_header_value_metadata() => _result.HeaderValuesSet.ShouldBeEmpty();
}
