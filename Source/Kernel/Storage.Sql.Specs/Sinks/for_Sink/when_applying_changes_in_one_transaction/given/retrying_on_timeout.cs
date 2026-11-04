// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction.given;

/// <summary>
/// Gives the SQLite specs the execution strategy the network providers have: a timeout is retried, as a lost commit
/// acknowledgment would be. SQLite itself has no retrying strategy, so without this the receipt could not be exercised.
/// </summary>
public static class retrying_on_timeout
{
    public static void Configure(DbContextOptionsBuilder<ReadModelDbContext> builder) =>
        builder.ReplaceService<IExecutionStrategyFactory, Factory>();

    sealed class Factory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new Strategy(dependencies);
    }

    sealed class Strategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 3, TimeSpan.FromMilliseconds(5))
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is TimeoutException || (exception.InnerException is not null && ShouldRetryOn(exception.InnerException));
    }
}
