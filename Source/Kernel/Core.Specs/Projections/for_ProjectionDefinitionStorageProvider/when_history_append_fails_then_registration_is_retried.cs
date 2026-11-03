// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionDefinitionStorageProvider;

public class when_history_append_fails_then_registration_is_retried : given.a_history_provider
{
    void Establish() => _history.Record(Arg.Any<ProjectionDefinitionHistoryEntry>()).Returns(_ => Task.FromException(new InvalidOperationException("history failed")), _ => Task.CompletedTask);
    async Task Because()
    {
        await Catch.Exception(Write);
        await Write();
    }

    [Fact] Task should_retry_the_history_write_because_the_failure_was_not_cached() => _history.Received(2).Record(Arg.Any<ProjectionDefinitionHistoryEntry>());
}
