// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_deep_self_referential_children.and_a_third_level_sub_feature_is_added.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_deep_self_referential_children;

[Collection(ChronicleCollection.Name)]
public class and_a_third_level_sub_feature_is_added(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_recursive_module(fixture)
    {
        async Task Because()
        {
            await Append(AddedId, new SubFeatureAdded(SecondId, "Third"));
            await ReadResult();
        }
    }

    RecursiveFeature Root => Context.Result.Features.Single();
    RecursiveFeature First => Root.SubFeatures.Single();
    RecursiveFeature Second => First.SubFeatures.Single();

    [Fact] void should_keep_only_the_root_at_module_level() => Root.Id.ShouldEqual(Context.RootId);
    [Fact] void should_keep_the_first_level_under_the_root() => First.Id.ShouldEqual(Context.FirstId);
    [Fact] void should_keep_the_second_level_under_the_first() => Second.Id.ShouldEqual(Context.SecondId);
    [Fact] void should_add_the_third_level_under_its_real_parent() => Second.SubFeatures.Single().Id.ShouldEqual(Context.AddedId);
    [Fact] void should_set_the_third_level_name() => Second.SubFeatures.Single().Name.ShouldEqual("Third");
}
