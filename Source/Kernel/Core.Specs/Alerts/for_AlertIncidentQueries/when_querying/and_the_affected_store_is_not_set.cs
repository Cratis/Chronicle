// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries.when_querying;

public class and_the_affected_store_is_not_set : given.scoped_queries
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => AlertIncidentSummary.GetOpenIncidentCounts(EventStoreName.NotSet, _storage, _readiness));

    [Fact] void should_reject_the_invalid_scope() => _error.ShouldBeOfExactType<InvalidAlertIncidentQuery>();
}
