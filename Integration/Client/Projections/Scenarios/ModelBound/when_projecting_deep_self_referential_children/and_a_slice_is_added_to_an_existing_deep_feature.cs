// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_deep_self_referential_children.and_a_slice_is_added_to_an_existing_deep_feature.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_deep_self_referential_children;

[Collection(ChronicleCollection.Name)]
public class and_a_slice_is_added_to_an_existing_deep_feature(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_recursive_module(fixture)
    {
        public RecursiveModule Before;

        void Establish() => Before = Result;

        async Task Because()
        {
            await Append(AddedId, new SliceAddedToFeature(SecondId, AddedId, "Deep slice"));
            await ReadResult();
        }
    }

    RecursiveFeature Root => Context.Result.Features.Single();
    RecursiveFeature First => Root.SubFeatures.Single();
    RecursiveFeature Second => First.SubFeatures.Single();

    [Fact] void should_start_with_the_existing_deep_feature() => Context.Before.Features.Single().SubFeatures.Single().SubFeatures.Single().Id.ShouldEqual(Context.SecondId);
    [Fact] void should_start_without_slices_on_the_deep_feature() => Context.Before.Features.Single().SubFeatures.Single().SubFeatures.Single().Slices.ShouldBeEmpty();
    [Fact] void should_keep_the_root_identity() => Root.Id.ShouldEqual(Context.RootId);
    [Fact] void should_keep_the_first_level_identity() => First.Id.ShouldEqual(Context.FirstId);
    [Fact] void should_keep_the_second_level_identity() => Second.Id.ShouldEqual(Context.SecondId);
    [Fact] void should_not_add_the_slice_to_the_root() => Root.Slices.ShouldBeEmpty();
    [Fact] void should_not_add_the_slice_to_the_first_level() => First.Slices.ShouldBeEmpty();
    [Fact] void should_add_the_slice_to_the_deep_feature() => Second.Slices.Single().Id.ShouldEqual(Context.AddedId);
    [Fact] void should_set_the_slice_name() => Second.Slices.Single().Name.ShouldEqual("Deep slice");
}
