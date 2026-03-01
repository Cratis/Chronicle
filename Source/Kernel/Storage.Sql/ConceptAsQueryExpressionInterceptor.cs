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
        // FIRST: Evaluate ConceptAs closure variables to constants
        // This prevents EF Core from creating ConceptAs parameters
        var evaluated = ConceptAsParameterEvaluator.Evaluate(queryExpression);

        // SECOND: Rewrite the expression to handle any remaining ConceptAs types
        return ConceptAsExpressionRewriter.Rewrite(evaluated);
    }
}
