// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating;

public class and_type_is_empty_and_connection_is_unrecognizable : Specification
{
    List<ValidationResult> _errors;

    void Because()
    {
        var options = new ChronicleOptions { Storage = new() { ConnectionDetails = "something-odd" } };
        _errors = [.. options.Validate(new ValidationContext(options))];
    }

    [Fact] void should_flag_the_type() => _errors.SelectMany(_ => _.MemberNames).ShouldContain("Storage.Type");
    [Fact] void should_name_the_environment_variable() => _errors.Select(_ => _.ErrorMessage).ShouldContain(_ => _!.Contains("Cratis__Chronicle__Storage__Type"));
}
