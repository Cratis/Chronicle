// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Interceptor that rewrites LINQ expressions to handle ConceptAs types before EF Core translates them to SQL.
/// </summary>
public class ConceptAsQueryExpressionInterceptor : IQueryExpressionInterceptor
{
    /// <inheritdoc/>
    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
    {
        Console.WriteLine($">>>>>>> ConceptAsQueryExpressionInterceptor: Rewriting expression: {queryExpression}");
        var result = ConceptAsExpressionRewriter.Rewrite(queryExpression);
        Console.WriteLine($">>>>>>> ConceptAsQueryExpressionInterceptor: Rewrite complete. Result: {result}");
        return result;
    }
}
