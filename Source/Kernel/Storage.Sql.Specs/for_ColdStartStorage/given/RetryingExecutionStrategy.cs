// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage.given;

/// <summary>
/// A retrying execution strategy for SQLite, so specs see the same refusal of user-initiated transactions
/// outside the strategy that SQL Server and PostgreSQL apply with retry on failure enabled.
/// </summary>
/// <param name="dependencies">The execution strategy dependencies.</param>
public class RetryingExecutionStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 3, TimeSpan.FromMilliseconds(10))
{
    /// <inheritdoc/>
    protected override bool ShouldRetryOn(Exception exception) => false;
}
