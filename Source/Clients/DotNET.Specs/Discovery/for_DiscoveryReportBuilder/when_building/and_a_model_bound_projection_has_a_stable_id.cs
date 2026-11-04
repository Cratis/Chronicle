// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Serialization;

namespace Cratis.Chronicle.Discovery.for_DiscoveryReportBuilder.when_building;

public class and_a_model_bound_projection_has_a_stable_id : given.a_report_builder
{
    async Task Establish()
    {
        _clientArtifactsProvider.ModelBoundProjections.Returns([typeof(RenamedModel)]);
        _projections.Add(new ModelBoundExplicitProjection(typeof(RenamedModel)));
        await _projections.Discover();
    }

    void Because() => _report = _builder.Build();

    [Fact] void should_report_the_stable_projection_id() => _report.OfKind(ArtifactKind.Projection).Single().Id.ShouldEqual("Former.Namespace.Model");
    [Fact] void should_report_the_renamed_clr_owner() => _report.OfKind(ArtifactKind.Projection).Single().ClrType.ShouldEqual(typeof(RenamedModel));
    [Fact] void should_keep_one_projection_for_the_overlay() => _report.CountOf(ArtifactKind.Projection).ShouldEqual(1);
    [Fact] void should_not_change_the_read_model_container() => _report.OfKind(ArtifactKind.ReadModel).Single().Id.ShouldEqual(new DefaultNamingPolicy().GetReadModelName(typeof(RenamedModel)));
    [Fact] void should_not_change_read_model_identity() => _projections.Definitions.Single().ReadModel.ShouldEqual(typeof(RenamedModel).GetReadModelIdentifier().Value);

    [Projection("Former.Namespace.Model")]
    [NotRewindable]
    public record RenamedModel(string Name);
}
