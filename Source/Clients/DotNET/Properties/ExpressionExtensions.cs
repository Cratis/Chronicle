// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Extension methods for <see cref="Expression"/>.
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// Get <see cref="PropertyPath"/> from an <see cref="Expression"/>.
    /// </summary>
    /// <param name="expression"><see cref="Expression"/> to get from.</param>
    /// <returns>The full <see cref="PropertyPath"/>.</returns>
    public static PropertyPath GetPropertyPath(this Expression expression)
    {
        if (expression is LambdaExpression lambda)
        {
            var current = lambda.Body;
            var members = new List<string>();
            if (current is UnaryExpression unary)
            {
                current = unary.Operand;
            }
            while (current is MemberExpression memberExpression)
            {
                current = memberExpression.Expression;
                members.Insert(0, memberExpression.Member.Name);
            }
            return new PropertyPath(string.Join('.', members));
         }

        return new PropertyPath(string.Empty);
     }
}
