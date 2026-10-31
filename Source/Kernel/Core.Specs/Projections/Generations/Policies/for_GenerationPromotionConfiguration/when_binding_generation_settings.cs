// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Projections.Generations.Policies.for_GenerationPromotionConfiguration;

public class when_binding_generation_settings : Specification
{
    ChronicleOptions _options;
    Dictionary<string, string?> _values;

    void Establish()
    {
        _options = new ChronicleOptions();
        _values = new()
        {
            ["Cratis:Chronicle:Observers:GenerationPromotion"] = "Operator",
            ["Cratis:Chronicle:Observers:DeploymentConditionGracePeriod"] = "00:02:00",
            ["Cratis:Chronicle:Observers:PendingGenerationAlertAfter"] = "02:00:00",
            ["Cratis:Chronicle:ReadModels:RollbackWindow"] = "06:00:00",
            ["Cratis:Chronicle:ReadModels:MaxFollowingRetiredGenerations"] = "3"
        };
    }

    void Because() => new ConfigurationBuilder().AddInMemoryCollection(_values).Build().GetSection(ChronicleOptions.SectionPath).Bind(_options);

    [Fact] void should_bind_the_gate() => _options.Observers.GenerationPromotion.ShouldEqual(PromotionGateMode.Operator);
    [Fact] void should_bind_the_grace_period() => _options.Observers.DeploymentConditionGracePeriod.ShouldEqual(TimeSpan.FromMinutes(2));
    [Fact] void should_bind_the_pending_alert_delay() => _options.Observers.PendingGenerationAlertAfter.ShouldEqual(TimeSpan.FromHours(2));
    [Fact] void should_bind_the_rollback_window() => _options.ReadModels.RollbackWindow.ShouldEqual(TimeSpan.FromHours(6));
    [Fact] void should_bind_the_retired_follower_limit() => _options.ReadModels.MaxFollowingRetiredGenerations.ShouldEqual(3);
}
