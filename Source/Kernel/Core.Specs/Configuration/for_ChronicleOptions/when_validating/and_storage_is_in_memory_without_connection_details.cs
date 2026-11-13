// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating;

public class and_storage_is_in_memory_without_connection_details : Specification
{
    List<ValidationResult> _errors;

    void Because()
    {
        var options = new ChronicleOptions { Storage = new() { Type = StorageType.InMemory } };
        _errors = [.. options.Validate(new ValidationContext(options))];
    }

    [Fact] void should_not_require_connection_details() => _errors.SelectMany(_ => _.MemberNames).ShouldNotContain("Storage.ConnectionDetails");
}
