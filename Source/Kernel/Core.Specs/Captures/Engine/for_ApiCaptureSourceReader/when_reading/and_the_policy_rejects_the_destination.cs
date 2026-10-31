// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Captures.Engine.for_ApiCaptureSourceReader.when_reading;

public class and_the_policy_rejects_the_destination : given.an_api_capture_source_reader
{
    Exception _exception;

    void Establish()
    {
        _policy.CheckAuthority(Arg.Any<Uri>(), Arg.Any<EventStoreName?>()).Returns(new WebhookTargetValidation(false, "Metadata is not approved."));
    }

    async Task Because() => _exception = await Catch.Exception(() => _reader.Read(_eventStore, _source));

    [Fact] void should_reject_before_sending() => _exception.GetType().ShouldEqual(typeof(WebhookTargetRejected));
    [Fact] void should_not_send_the_request() => _request.ShouldBeNull();
}
