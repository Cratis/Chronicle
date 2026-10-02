// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

/// <summary>
/// Alerts bind from configuration, including condition names with hyphens as keys, so an operator can tune each
/// condition from a file or an environment variable.
/// </summary>
public class and_alerts_are_configured : Specification
{
    ChronicleOptions _options;

    void Establish()
    {
        _options = new ChronicleOptions();
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Chronicle:Alerts:Enabled"] = "false",
                ["Cratis:Chronicle:Alerts:Conditions:partition-failing:RaiseAfter"] = "00:10:00",
                ["Cratis:Chronicle:Alerts:Conditions:partition-failing:Severity"] = "Critical",
                ["Cratis:Chronicle:Alerts:Conditions:observer-quarantined:Enabled"] = "false",
                ["Cratis:Chronicle:Alerts:ExcludeObservers:0"] = "dev.*",
                ["Cratis:Chronicle:Alerts:ExcludeObservers:1"] = "batch-?"
            })
            .Build()
            .GetSection(ChronicleOptions.SectionPath)
            .Bind(_options);
    }

    [Fact] void should_bind_whether_alerts_are_enabled() => _options.Alerts.Enabled.ShouldBeFalse();
    [Fact] void should_bind_both_conditions_by_their_hyphenated_names() => _options.Alerts.Conditions.Keys.ShouldContainOnly("partition-failing", "observer-quarantined");
    [Fact] void should_bind_the_raise_after() => _options.Alerts.Conditions["partition-failing"].RaiseAfter.ShouldEqual(TimeSpan.FromMinutes(10));
    [Fact] void should_bind_the_severity() => _options.Alerts.Conditions["partition-failing"].Severity.ShouldEqual(AlertSeverity.Critical);
    [Fact] void should_bind_a_disabled_condition() => _options.Alerts.Conditions["observer-quarantined"].Enabled.ShouldBeFalse();
    [Fact] void should_keep_the_default_for_a_setting_that_is_not_configured() => _options.Alerts.Conditions["observer-quarantined"].Severity.ShouldBeNull();
    [Fact] void should_bind_the_excluded_observers() => _options.Alerts.ExcludeObservers.ShouldContainOnly("dev.*", "batch-?");
    [Fact] void should_look_up_conditions_without_regard_to_case() => _options.Alerts.Conditions.ContainsKey("PARTITION-FAILING").ShouldBeTrue();
}
