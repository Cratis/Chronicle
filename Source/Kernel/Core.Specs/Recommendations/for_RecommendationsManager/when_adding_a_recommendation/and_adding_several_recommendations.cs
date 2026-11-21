// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Recommendations.for_RecommendationsManager.when_adding_a_recommendation;

public class and_adding_several_recommendations : given.a_definition_change
{
    async Task Because()
    {
        await Add("a");
        await Add("b");
        await Add("c");
    }

    [Fact] void should_load_the_index_only_once() => _recommendationStorage.Received(1).GetAll();
}
