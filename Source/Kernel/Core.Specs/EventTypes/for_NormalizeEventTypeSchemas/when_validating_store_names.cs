// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation.Results;

namespace Cratis.Chronicle.EventTypes.for_NormalizeEventTypeSchemas;

public class when_validating_store_names : Specification
{
    ValidationResult[] _invalid;
    ValidationResult _valid;

    async Task Because()
    {
        var validator = new NormalizeEventTypeSchemasValidator();
        _invalid = await Task.WhenAll(new[] { "", "System", "system", "x#System" }
            .Select(store => validator.ValidateAsync(new NormalizeEventTypeSchemas(store))));
        _valid = await validator.ValidateAsync(new NormalizeEventTypeSchemas("application"));
    }

    [Fact] void should_reject_invalid_stores() => _invalid.All(result => !result.IsValid).ShouldBeTrue();
    [Fact] void should_accept_application_store() => _valid.IsValid.ShouldBeTrue();
}
