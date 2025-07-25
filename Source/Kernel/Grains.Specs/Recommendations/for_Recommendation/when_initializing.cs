// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;

namespace Cratis.Chronicle.Grains.Recommendations.for_Recommendation;

public class when_initializing : given.all_dependencies
{
    static given.TheRequest _request;
    static Exception _error;

    void Establish()
    {
        _request = new given.TheRequest(42);
        description = "Some description";
    }

    async Task Because() => _error = await Catch.Exception(() => recommendation.Initialize(description, _request));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_store_the_correct_description() => storage.State.Description.ShouldEqual(description);
    [Fact] void should_store_the_correct_name() => storage.State.Name.Value.ShouldEqual(recommendation.GetType().Name);
    [Fact] void should_store_the_correct_type() => storage.State.Type.Value.ShouldEqual((RecommendationType)recommendation.GrainType);
    [Fact] void should_store_the_correct_request() => storage.State.Request.ShouldEqual(_request);
    [Fact] void should_write_to_state_once() => storageStats.Writes.ShouldEqual(1);
    [Fact] void should_not_clear_state() => storageStats.Clears.ShouldEqual(0);
}
