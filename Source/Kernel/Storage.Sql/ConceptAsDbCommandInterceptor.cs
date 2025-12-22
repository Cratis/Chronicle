// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Interceptor that unwraps ConceptAs parameter values to their underlying primitive values before SQL execution.
/// </summary>
public class ConceptAsDbCommandInterceptor : DbCommandInterceptor
{
    /// <inheritdoc/>
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        UnwrapConceptAsParameters(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        UnwrapConceptAsParameters(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        UnwrapConceptAsParameters(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        UnwrapConceptAsParameters(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        UnwrapConceptAsParameters(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UnwrapConceptAsParameters(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    static void UnwrapConceptAsParameters(DbCommand command)
    {
        foreach (DbParameter parameter in command.Parameters)
        {
            if (parameter.Value?.GetType().IsConcept() == true)
            {
                var conceptType = parameter.Value.GetType();
                var valueProperty = conceptType.GetProperty("Value");

                if (valueProperty is not null)
                {
                    var unwrappedValue = valueProperty.GetValue(parameter.Value);
                    parameter.Value = unwrappedValue;
                }
            }
        }
    }
}
