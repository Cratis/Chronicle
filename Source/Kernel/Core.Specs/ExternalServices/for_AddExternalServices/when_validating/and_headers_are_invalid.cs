// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ExternalServices;
using FluentValidation.Results;

namespace Cratis.Chronicle.ExternalServices.for_AddExternalServices.when_validating;

public class and_headers_are_invalid : Helpers.for_CustomHttpHeaders.given.invalid_headers
{
    ValidationResult[] _results;

    void Because()
    {
        var validator = new AddExternalServicesValidator();
        _results = _invalidHeaders.Select(headers => validator.Validate(new AddExternalServices("store",
        [
            new ExternalServiceDefinition
            {
                Id = "service",
                Name = "service",
                Endpoint = new() { Type = ExternalServiceEndpointType.Http, Http = new() { Url = "https://receiver.example", Headers = headers } }
            }
        ]))).ToArray();
    }

    [Fact] void should_reject_every_invalid_header() => _results.All(result => !result.IsValid).ShouldBeTrue();
    [Fact] void should_identify_the_headers() => _results.All(result => result.Errors.Exists(error => error.PropertyName.Contains("Headers", StringComparison.Ordinal))).ShouldBeTrue();
    [Fact] void should_not_include_the_value_in_validation_errors() => _results.All(result => !result.ToString().Contains(Secret)).ShouldBeTrue();
}
