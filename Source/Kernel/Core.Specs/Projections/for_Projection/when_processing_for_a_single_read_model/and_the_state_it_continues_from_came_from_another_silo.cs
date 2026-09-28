// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Setup.Serialization;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_a_single_read_model;

/// <summary>
/// An on-demand read projects on the silo that holds the projection and caches the result on the silo that
/// holds the read, then hands the cached state back to project further events onto. This is that whole trip,
/// through the kernel's own serializer - the path that threw, and then left the projection returning nothing
/// for every instance, in production (Cratis/Chronicle#4337).
/// </summary>
public class and_the_state_it_continues_from_came_from_another_silo : given.a_projection_grain_with_a_children_projection
{
    ExpandoObject _continued;
    ExpandoObject _another;

    async Task Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new JsonSerializerOptions());
        services.AddSingleton(Substitute.For<IExpandoObjectConverter>());
        services.AddSingleton(Substitute.For<IStorage>());
        services.AddSerializer(builder => builder.Services.AddCustomSerializers());
        var serializer = services.BuildServiceProvider().GetRequiredService<Serializer>();

        var folded = await Fold(
            Associated("issue", 1, "comment-1", "100"),
            Associated("issue", 2, "comment-2", "200"));
        var cachedOnAnotherSilo = serializer.Deserialize<ExpandoObject>(serializer.SerializeToArray(folded)!)!;

        _continued = await FoldOnto(cachedOnAnotherSilo, Associated("issue", 3, "comment-1", "101"));
        _another = await Fold(Associated("another-issue", 4, "comment-3", "300"));
    }

    [Fact] void should_hold_each_child_once() => AssociationsOf(_continued).Select(_ => _["id"]).ShouldContainOnly("comment-1", "comment-2");
    [Fact] void should_update_the_repeated_child() => AssociationsOf(_continued).Single(_ => Equals(_["id"], "comment-1"))["reference"].ShouldEqual("101");
    [Fact] void should_still_project_another_instance() => AssociationsOf(_another).Single()["reference"].ShouldEqual("300");
}
