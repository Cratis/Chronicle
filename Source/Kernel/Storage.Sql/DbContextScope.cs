// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents a database context scope.
/// </summary>
/// <param name="DbContext">The database context.</param>
/// <param name="OnDispose">Action to execute on dispose.</param>
/// <typeparam name="T">The type of the DbContext.</typeparam>
public record DbContextScope<T>(T DbContext, Action OnDispose) : IAsyncDisposable
    where T : DbContext
{
    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        OnDispose?.Invoke();
        await DbContext.DisposeAsync();
    }
}
