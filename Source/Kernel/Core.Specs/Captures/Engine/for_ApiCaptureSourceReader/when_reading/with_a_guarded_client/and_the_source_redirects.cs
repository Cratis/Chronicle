// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Captures.Engine.for_ApiCaptureSourceReader.when_reading.with_a_guarded_client;

public class and_the_source_redirects : given.a_reader_with_a_guarded_client
{
    Exception _exception;

    void Establish()
    {
        _transport.StatusCode = HttpStatusCode.Redirect;
        _transport.Location = new Uri("http://169.254.169.254/");
        Configure(new() { Validation = WebhookValidationMode.BlockLocalAddresses });
    }

    async Task Because() => _exception = await Catch.Exception(() => _reader.Read(_eventStore, _source));

    [Fact] void should_refuse_the_redirect() => _exception.ShouldBeOfExactType<WebhookTargetRejected>();
    [Fact] void should_send_only_the_initial_request() => _transport.Requests.Count.ShouldEqual(1);
    [Fact] void should_send_to_the_registered_origin() => _transport.Requests[0].Target.ShouldEqual(new Uri("https://capture.example/items"));
    [Fact] void should_apply_authorization_only_to_the_initial_request() => _transport.Requests[0].Authorization.ShouldEqual("Bearer token");
    [Fact] void should_apply_the_custom_header_only_to_the_initial_request() => _transport.Requests[0].HasApiKey.ShouldBeTrue();
}
