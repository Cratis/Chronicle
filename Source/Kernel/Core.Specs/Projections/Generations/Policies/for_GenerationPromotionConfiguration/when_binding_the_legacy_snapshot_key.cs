// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Projections.Generations.Policies.for_GenerationPromotionConfiguration;

public class when_binding_the_legacy_snapshot_key : Specification
{
    ChronicleOptions _options;
    Dictionary<string, string?> _values;

    void Establish()
    {
        _options = new ChronicleOptions();
        _values = new() { ["Cratis:Chronicle:ReadModels:ReplayedVersionsToKeep"] = "4" };
    }

    void Because() => new ConfigurationBuilder().AddInMemoryCollection(_values).Build().GetSection(ChronicleOptions.SectionPath).Bind(_options);

    [Fact] void should_bind_the_snapshot_count() => _options.ReadModels.SnapshotsToKeep.ShouldEqual(4);
    [Fact] void should_bind_the_legacy_snapshot_count() => _options.ReadModels.ReplayedVersionsToKeep.ShouldEqual(4);
}
