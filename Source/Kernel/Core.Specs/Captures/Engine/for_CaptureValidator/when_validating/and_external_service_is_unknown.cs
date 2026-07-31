// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.when_validating;

public class and_external_service_is_unknown : given.a_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    async Task Because() => _result = await _validator.Validate(
        _eventStore,
        CreateDefinition(source: new SourceDefinition(SourceType.Api, Api: "UnknownApi", Poll: "5m")));

    [Fact] void should_have_one_message() => _result.Count().ShouldEqual(1);
    [Fact] void should_point_out_the_unknown_service() => _result.First().Message.ShouldContain("UnknownApi");
}
