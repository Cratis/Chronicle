// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ExternalServices;
using Cratis.Chronicle.Contracts.Security;
using FluentValidation.Results;

namespace Cratis.Chronicle.ExternalServices.for_AddExternalService.when_validating;

public class and_headers_are_invalid : Helpers.for_CustomHttpHeaders.given.invalid_headers
{
    ValidationResult[] _results;

    void Because()
    {
        var validator = new AddExternalServiceValidator();
        _results = _invalidHeaders.Select(headers => validator.Validate(new AddExternalService("store", "service", "service", ExternalServiceEndpointType.Http, "https://receiver.example", AuthorizationType.None, "", "", "", "", "", "", headers, "", 0, "", "", "", new Dictionary<string, string>()))).ToArray();
    }

    [Fact] void should_reject_every_invalid_header() => _results.All(result => !result.IsValid).ShouldBeTrue();
    [Fact] void should_identify_the_headers() => _results.All(result => result.Errors.Exists(error => error.PropertyName.StartsWith("Headers", StringComparison.Ordinal))).ShouldBeTrue();
    [Fact] void should_not_include_the_value_in_validation_errors() => _results.All(result => !result.ToString().Contains(Secret)).ShouldBeTrue();
}
