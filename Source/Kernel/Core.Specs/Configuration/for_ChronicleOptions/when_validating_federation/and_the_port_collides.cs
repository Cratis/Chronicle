// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_validating_federation;

public class and_the_port_collides : Specification
{
    IEnumerable<ValidationResult> _collidingWithMain;
    IEnumerable<ValidationResult> _collidingWithHealth;
    IEnumerable<ValidationResult> _separate;
    IEnumerable<ValidationResult> _disabled;

    void Because()
    {
        _collidingWithMain = Validate(new() { Port = 35000, Federation = new() { Enabled = true, Port = 35000 } });
        _collidingWithHealth = Validate(new() { Health = new() { Port = 8080 }, Federation = new() { Enabled = true, Port = 8080 } });
        _separate = Validate(new() { Federation = new() { Enabled = true } });
        _disabled = Validate(new() { Port = 35000, Federation = new() { Enabled = false, Port = 35000 } });
    }

    static List<ValidationResult> Validate(ChronicleOptions options) => [.. options.Validate(new ValidationContext(options))];

    [Fact] void should_reject_the_main_port() => _collidingWithMain.ShouldContain(_ => _.MemberNames.Contains("Federation.Port"));
    [Fact] void should_reject_the_health_port() => _collidingWithHealth.ShouldContain(_ => _.MemberNames.Contains("Federation.Port"));
    [Fact] void should_accept_a_separate_port() => _separate.ShouldBeEmpty();
    [Fact] void should_not_validate_the_port_when_disabled() => _disabled.ShouldBeEmpty();
}
