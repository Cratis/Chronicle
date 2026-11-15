// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating;

public class and_connection_details_are_supplied_by_a_file : Specification
{
    List<ValidationResult> _errors;

    void Because()
    {
        var options = new ChronicleOptions { Storage = new() { ConnectionDetailsFile = "/run/secrets/chronicle-storage" } };
        _errors = [.. options.Validate(new ValidationContext(options))];
    }

    [Fact] void should_not_complain_about_storage() => _errors.SelectMany(_ => _.MemberNames).Any(_ => _.StartsWith("Storage")).ShouldBeFalse();
}
