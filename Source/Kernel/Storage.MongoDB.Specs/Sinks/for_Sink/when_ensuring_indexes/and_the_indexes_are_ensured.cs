// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_the_indexes_are_ensured : given.a_sink_with_indexes
{
    Task Because() => _sink.EnsureIndexes();

    [Fact] void should_clear_failures_for_the_collection() => _authorizationDiagnostics.Received(1).Clear("System", "EventTypeStatistics");
}
