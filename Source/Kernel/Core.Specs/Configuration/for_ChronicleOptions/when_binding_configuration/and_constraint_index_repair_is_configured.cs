// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

public class and_constraint_index_repair_is_configured : Specification
{
    ChronicleOptions _options;

    void Establish()
    {
        _options = new ChronicleOptions();
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Chronicle:ConstraintIndexRepair:UpdateAttempts"] = "7",
                ["Cratis:Chronicle:ConstraintIndexRepair:AppendRepairBudget"] = "00:00:09"
            })
            .Build()
            .GetSection(ChronicleOptions.SectionPath)
            .Bind(_options);
    }

    [Fact] void should_bind_the_update_attempts() => _options.ConstraintIndexRepair.UpdateAttempts.ShouldEqual(7);
    [Fact] void should_bind_the_append_repair_budget() => _options.ConstraintIndexRepair.AppendRepairBudget.ShouldEqual(TimeSpan.FromSeconds(9));
    [Fact] void should_keep_the_default_for_what_is_not_configured() => _options.ConstraintIndexRepair.PoisonThreshold.ShouldEqual(5);
}
