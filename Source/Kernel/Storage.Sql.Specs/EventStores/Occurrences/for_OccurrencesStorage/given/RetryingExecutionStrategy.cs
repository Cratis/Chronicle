// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_OccurrencesStorage.given;

/// <summary>
/// A retrying execution strategy that, like the PostgreSQL one, refuses commands in a transaction it did not start.
/// </summary>
/// <param name="dependencies">The strategy dependencies.</param>
public class RetryingExecutionStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 1, TimeSpan.Zero)
{
    /// <inheritdoc/>
    protected override bool ShouldRetryOn(Exception exception) => false;
}
