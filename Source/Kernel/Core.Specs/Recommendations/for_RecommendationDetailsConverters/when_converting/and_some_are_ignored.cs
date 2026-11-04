// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Recommendations;

namespace Cratis.Chronicle.Recommendations.for_RecommendationDetailsConverters.when_converting;

public class and_some_are_ignored : Specification
{
    RecommendationState _active;
    RecommendationState _ignored;
    IEnumerable<RecommendationDetails> _activeDetails;
    IEnumerable<RecommendationDetails> _ignoredDetails;

    void Establish()
    {
        _active = new RecommendationState { Id = Guid.NewGuid() };
        _ignored = new RecommendationState { Id = Guid.NewGuid(), IsIgnored = true };
    }

    void Because()
    {
        RecommendationState[] all = [_active, _ignored];
        _activeDetails = all.ToDetails();
        _ignoredDetails = all.ToIgnoredDetails();
    }

    [Fact] void should_only_include_active_in_the_active_details() => _activeDetails.Select(_ => _.Id).ShouldContainOnly([_active.Id.Value]);
    [Fact] void should_only_include_ignored_in_the_ignored_details() => _ignoredDetails.Select(_ => _.Id).ShouldContainOnly([_ignored.Id.Value]);
}
