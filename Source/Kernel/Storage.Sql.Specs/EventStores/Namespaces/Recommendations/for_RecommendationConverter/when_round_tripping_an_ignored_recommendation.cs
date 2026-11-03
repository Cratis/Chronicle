// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Recommendations.for_RecommendationConverter;

public class when_round_tripping_an_ignored_recommendation : Specification
{
    RecommendationConverter _converter;
    RecommendationId _id;
    Recommendation _entity;
    RecommendationState _result;
    RecommendationState _activeResult;

    void Establish()
    {
        _converter = new();
        _id = RecommendationId.New();
    }

    void Because()
    {
        _entity = _converter.ToEntity(_id, new RecommendationState { Id = _id, IsIgnored = true });
        _result = _converter.ToRecommendationState(_entity);
        _activeResult = _converter.ToRecommendationState(_converter.ToEntity(_id, new RecommendationState { Id = _id }));
    }

    [Fact] void should_carry_the_flag_on_the_entity() => _entity.IsIgnored.ShouldBeTrue();
    [Fact] void should_restore_the_flag() => _result.IsIgnored.ShouldBeTrue();
    [Fact] void should_keep_an_active_recommendation_active() => _activeResult.IsIgnored.ShouldBeFalse();
}
