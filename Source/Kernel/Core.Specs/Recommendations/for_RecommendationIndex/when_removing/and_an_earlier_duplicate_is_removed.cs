// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Recommendations.for_RecommendationIndex.when_removing;

public class and_an_earlier_duplicate_is_removed : Specification
{
    readonly RecommendationId _earlier = RecommendationId.New();
    readonly RecommendationId _latest = RecommendationId.New();
    readonly RecommendationType _type = (RecommendationType)typeof(for_RecommendationsManager.given.ITheRecommendation);
    readonly for_RecommendationsManager.given.TheRequest _request = new(42);
    IRecommendationStorage _storage;
    RecommendationIndex _index;
    RecommendationId? _result;

    async Task Establish()
    {
        _storage = Substitute.For<IRecommendationStorage>();
        var earlier = new RecommendationState { Id = _earlier, Type = _type, Request = _request };
        var latest = new RecommendationState { Id = _latest, Type = _type, Request = _request };
        _storage.GetAll().Returns(ImmutableList.Create(earlier, latest));
        _storage.Get(_earlier).Returns(earlier);
        _storage.Get(_latest).Returns(latest);
        _index = new RecommendationIndex(_storage);
        await _index.Find(_type, _request);
    }

    async Task Because()
    {
        _index.Remove(_earlier);
        _result = await _index.Find(_type, _request);
    }

    [Fact] void should_keep_the_latest_matching_row_indexed() => _result.ShouldEqual(_latest);
    [Fact] void should_verify_the_latest_row_in_storage() => _storage.Received(2).Get(_latest);
    [Fact] void should_not_reload_the_index() => _storage.Received(1).GetAll();
}
