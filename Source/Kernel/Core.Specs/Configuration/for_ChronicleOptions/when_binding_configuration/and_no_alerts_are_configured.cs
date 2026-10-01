// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

/// <summary>
/// Without any alert configuration alerts are enabled, no condition has an entry and no observer is excluded, so the
/// built-in defaults apply.
/// </summary>
public class and_no_alerts_are_configured : Specification
{
    ChronicleOptions _options;

    void Establish() => _options = new ChronicleOptions();

    [Fact] void should_enable_alerts() => _options.Alerts.Enabled.ShouldBeTrue();
    [Fact] void should_have_no_condition_entries() => _options.Alerts.Conditions.ShouldBeEmpty();
    [Fact] void should_exclude_no_observers() => _options.Alerts.ExcludeObservers.ShouldBeEmpty();
}
