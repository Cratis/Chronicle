// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.when_validating;

public class and_external_service_is_not_http : given.a_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    void Establish() =>
        _externalServices.GetAll().Returns(
        [
            new ExternalServiceDefinition(
                "customers-database",
                "CustomersDatabase",
                new ExternalServiceEndpoint(ExternalServiceEndpointType.PostgreSql))
        ]);

    async Task Because() => _result = await _validator.Validate(
        _eventStore,
        CreateDefinition(source: new SourceDefinition(SourceType.Api, Api: "CustomersDatabase", Poll: "5m")));

    [Fact] void should_have_one_message() => _result.Count().ShouldEqual(1);
    [Fact] void should_point_out_it_is_not_http() => _result.First().Message.ShouldContain("not an HTTP service");
}
