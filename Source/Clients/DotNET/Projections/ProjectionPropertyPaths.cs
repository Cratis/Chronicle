// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Resolves property names within a projection path using the serialization naming policy.
/// </summary>
internal static class ProjectionPropertyPaths
{
    /// <summary>
    /// Applies the naming policy to each property segment without changing path operators.
    /// </summary>
    /// <param name="path">The property path to resolve.</param>
    /// <param name="namingPolicy">The serialization naming policy.</param>
    /// <returns>The serialized property path.</returns>
    public static PropertyPath Resolve(PropertyPath path, INamingPolicy namingPolicy) =>
        new(string.Join('.', path.Segments.Select(segment => ResolveSegment(segment, namingPolicy))));

    /// <summary>
    /// Converts a member expression to its serialized projection property path.
    /// </summary>
    /// <param name="expression">The member expression.</param>
    /// <param name="namingPolicy">The serialization naming policy.</param>
    /// <returns>The serialized property path.</returns>
    public static PropertyPath FromExpression(LambdaExpression expression, INamingPolicy namingPolicy) =>
        Resolve(expression.GetPropertyPath(), namingPolicy);

    /// <summary>
    /// Converts a member expression and retains the caller's invalid-expression description.
    /// </summary>
    /// <param name="expression">The member expression.</param>
    /// <param name="namingPolicy">The serialization naming policy.</param>
    /// <param name="description">The description of the property being configured.</param>
    /// <returns>The serialized property path.</returns>
    /// <exception cref="InvalidPropertyExpression">The expression is not a property path.</exception>
    public static PropertyPath FromExpression(LambdaExpression expression, INamingPolicy namingPolicy, string description)
    {
        if (!expression.TryGetPropertyPath(out var path))
        {
            throw new InvalidPropertyExpression(description, expression);
        }

        return Resolve(path, namingPolicy);
    }

    static string ResolveSegment(IPropertyPathSegment segment, INamingPolicy namingPolicy) => segment switch
    {
        PropertyName => namingPolicy.GetPropertyName(segment.Value),
        ArrayProperty => $"[{namingPolicy.GetPropertyName(segment.Value)}]",
        _ => segment.ToString()!
    };
}
