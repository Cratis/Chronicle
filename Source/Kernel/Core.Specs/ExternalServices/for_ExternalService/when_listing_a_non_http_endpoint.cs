// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.ExternalServices.for_ExternalService;

public class when_listing_a_non_http_endpoint : Specification
{
    ExternalService _result;

    void Because() => _result = new ExternalServiceDefinition("database", "Database", new(ExternalServiceEndpointType.MsSql)).ToReadModel();

    [Fact] void should_not_have_header_names() => _result.Headers.ShouldBeEmpty();
    [Fact] void should_not_have_header_value_metadata() => _result.HeaderValuesSet.ShouldBeEmpty();
}
