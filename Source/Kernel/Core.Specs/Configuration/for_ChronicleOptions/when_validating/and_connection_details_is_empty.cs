// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating;

public class and_connection_details_is_empty : Specification
{
    List<ValidationResult> _errors;

    void Because()
    {
        var options = new ChronicleOptions();
        _errors = [.. options.Validate(new ValidationContext(options))];
    }

    [Fact] void should_require_the_connection_details() => _errors.SelectMany(_ => _.MemberNames).ShouldContain("Storage.ConnectionDetails");
    [Fact] void should_name_the_environment_variable() => _errors.Select(_ => _.ErrorMessage).ShouldContain(_ => _!.Contains("Cratis__Chronicle__Storage__ConnectionDetails"));
}
