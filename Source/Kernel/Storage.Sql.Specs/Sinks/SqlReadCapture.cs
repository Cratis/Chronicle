// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Data.Common;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

public class SqlReadCapture : DbCommandInterceptor, IMaterializationInterceptor
{
    readonly ConcurrentQueue<string> _commands = new();
    int _rows;
    public IReadOnlyList<string> Commands => _commands.ToArray();
    public int Rows => _rows;

    public void Clear()
    {
        _commands.Clear();
        _rows = 0;
    }

    public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
    {
        if (entity is DynamicReadModelEntity) Interlocked.Increment(ref _rows);
        return entity;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        _commands.Enqueue(command.CommandText);
        return ValueTask.FromResult(result);
    }
}
