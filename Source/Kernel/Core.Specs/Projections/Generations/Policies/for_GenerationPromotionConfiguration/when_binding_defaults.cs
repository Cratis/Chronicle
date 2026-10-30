// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Projections.Generations.Policies.for_GenerationPromotionConfiguration;

public class when_binding_defaults : Specification
{
    ChronicleOptions _options;
    Dictionary<string, string?> _values;

    void Establish()
    {
        _options = new ChronicleOptions();
        _values = [];
    }

    void Because() => new ConfigurationBuilder().AddInMemoryCollection(_values).Build().GetSection(ChronicleOptions.SectionPath).Bind(_options);

    [Fact] void should_leave_generations_disabled() => _options.Features.ProjectionGenerations.ShouldBeFalse();
    [Fact] void should_promote_on_catch_up() => _options.Observers.GenerationPromotion.ShouldEqual(PromotionGateMode.OnCatchUp);
    [Fact] void should_wait_sixty_seconds_for_deployment() => _options.Observers.DeploymentConditionGracePeriod.ShouldEqual(TimeSpan.FromSeconds(60));
    [Fact] void should_alert_after_a_day() => _options.Observers.PendingGenerationAlertAfter.ShouldEqual(TimeSpan.FromHours(24));
    [Fact] void should_retain_retired_followers_for_a_day() => _options.ReadModels.RollbackWindow.ShouldEqual(TimeSpan.FromHours(24));
    [Fact] void should_retain_one_retired_follower() => _options.ReadModels.MaxFollowingRetiredGenerations.ShouldEqual(1);
    [Fact] void should_retain_one_snapshot() => _options.ReadModels.SnapshotsToKeep.ShouldEqual(1);
    [Fact] void should_keep_the_legacy_snapshot_default() => _options.ReadModels.ReplayedVersionsToKeep.ShouldEqual(1);
}
