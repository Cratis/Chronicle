// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Configuration.for_Shutdown;

public class when_the_reserve_is_zero : Specification
{
    ChronicleOptions _options;
    IEnumerable<ValidationResult> _errors;

    void Establish() => _options = new() { Shutdown = new() { HostShutdownReserve = TimeSpan.Zero } };

    void Because() => _errors = _options.Validate(new ValidationContext(_options)).ToArray();

    [Fact] void should_reject_the_configuration() => _errors.ShouldContain(error => error.MemberNames.Contains("Shutdown.HostShutdownReserve"));
}
