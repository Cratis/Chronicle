// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Configuration.for_Shutdown;

public class when_the_drain_is_disabled : Specification
{
    ChronicleOptions _options;
    IEnumerable<ValidationResult> _errors;

    void Establish() => _options = new() { Shutdown = new() { DrainTimeout = TimeSpan.Zero } };

    void Because() => _errors = _options.Validate(new ValidationContext(_options)).ToArray();

    [Fact] void should_accept_the_configuration() => _errors.ShouldNotContain(error => error.MemberNames.Any(member => member.StartsWith("Shutdown", StringComparison.Ordinal)));
    [Fact] void should_preserve_the_host_stop_reserve() => _options.Shutdown.HostShutdownTimeout.ShouldEqual(TimeSpan.FromSeconds(5));
    [Fact] void should_not_wait_for_drain() => ShutdownDrainBudget.EffectiveTimeout(_options.Shutdown.DrainTimeout, _options.Shutdown.HostShutdownTimeout, _options.Shutdown.HostShutdownReserve).ShouldEqual(TimeSpan.Zero);
}
