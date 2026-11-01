// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Several kernels racing to create a SQL Server/PostgreSQL database or schema on cold start can make
/// the loser's own command time out or lose its connection - the storage provider's own transient
/// failure, not a sibling silo failing to answer. The registered <see cref="IStorageErrorClassifier"/>
/// is what recognizes it; without that, the unhandled failure used to terminate the whole host instead
/// of being ridden out like any other startup contention.
/// </summary>
public class and_sql_database_provisioning_is_temporarily_contended : given.a_startup_task
{
    Exception _error;

    void Establish()
    {
        _eventTypes.DiscoverAndRegister(Cratis.Chronicle.Concepts.EventStoreName.System).Returns(
            _ => Task.FromException(new InvalidOperationException("Execution Timeout Expired")),
            _ => Task.CompletedTask);
        _storageErrorClassifier.IsTransient(Arg.Any<Exception>()).Returns(true);
    }

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_not_fail_the_host() => _error.ShouldBeNull();
    [Fact] async Task should_have_tried_the_step_again() => await _eventTypes.Received(2).DiscoverAndRegister(Cratis.Chronicle.Concepts.EventStoreName.System);
}
