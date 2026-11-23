// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_deep_self_referential_children.and_a_third_level_sub_feature_is_removed.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_deep_self_referential_children;

[Collection(ChronicleCollection.Name)]
public class and_a_third_level_sub_feature_is_removed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_recursive_module(fixture)
    {
        public Guid SiblingId = Guid.NewGuid();
        public RecursiveModule Before;

        async Task Establish()
        {
            await Append(AddedId, new SubFeatureAdded(SecondId, "Third"));
            await Append(SiblingId, new SubFeatureAdded(SecondId, "Sibling"));
            await ReadResult();
            Before = Result;
        }

        async Task Because()
        {
            await Append(AddedId, new SubFeatureRemoved(SecondId));
            await ReadResult();
        }
    }

    RecursiveFeature Root => Context.Result.Features.Single();
    RecursiveFeature First => Root.SubFeatures.Single();
    RecursiveFeature Second => First.SubFeatures.Single();

    [Fact] void should_start_with_the_third_level_child() => Context.Before.Features.Single().SubFeatures.Single().SubFeatures.Single().SubFeatures.Any(_ => _.Id == Context.AddedId).ShouldBeTrue();
    [Fact] void should_keep_the_root_feature() => Root.Id.ShouldEqual(Context.RootId);
    [Fact] void should_keep_the_first_level_feature() => First.Id.ShouldEqual(Context.FirstId);
    [Fact] void should_keep_the_second_level_feature() => Second.Id.ShouldEqual(Context.SecondId);
    [Fact] void should_remove_only_the_requested_third_level_child() => Second.SubFeatures.Single().Id.ShouldEqual(Context.SiblingId);
    [Fact] void should_preserve_the_sibling_name() => Second.SubFeatures.Single().Name.ShouldEqual("Sibling");
}
