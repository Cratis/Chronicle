// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Data.Common;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Copies the first SELECT in a labelled, untimed operation.</summary>
internal sealed class QueryCapture
{
    readonly AsyncLocal<string?> _label = new();
    readonly ConcurrentDictionary<string, CapturedQuery> _queries = new(StringComparer.Ordinal);

    /// <summary>Gets the first SELECT captured under each label.</summary>
    public IReadOnlyDictionary<string, CapturedQuery> Queries => _queries;

    /// <summary>Labels commands in this asynchronous scope.</summary>
    /// <param name="label">The operation label.</param>
    /// <returns>A scope that restores the previous label.</returns>
    public IDisposable Begin(string label)
    {
        var previous = _label.Value;
        _label.Value = label;
        return new Scope(() => _label.Value = previous);
    }

    /// <summary>Copies the first SELECT, ignoring unlabelled commands.</summary>
    /// <param name="command">The command about to execute.</param>
    public void Record(DbCommand command)
    {
        var label = _label.Value;
        if (label is null) return;
        if (_queries.ContainsKey(label)) return;
        if (!command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)) return;
        var parameters = command.Parameters.Cast<DbParameter>().Select(parameter => new CapturedParameter(
            parameter.ParameterName,
            parameter.Value is Array values ? values.Clone() : parameter.Value,
            parameter.DbType,
            parameter switch
            {
                Npgsql.NpgsqlParameter postgres => postgres.Clone(),
                Microsoft.Data.SqlClient.SqlParameter sqlServer => (DbParameter)((ICloneable)sqlServer).Clone(),
                _ => null
            })).ToArray();
        _queries.TryAdd(label, new(command.CommandText, parameters));
    }

    sealed class Scope(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
