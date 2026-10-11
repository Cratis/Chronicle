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
    /// Applies the naming policy to a raw path, recognizing functions only by explicit parentheses.
    /// </summary>
    /// <param name="path">The raw property path to resolve.</param>
    /// <param name="namingPolicy">The serialization naming policy.</param>
    /// <returns>The serialized property path.</returns>
    public static PropertyPath Resolve(string path, INamingPolicy namingPolicy) =>
        new(string.Join('.', path.Split('.').Select(segment => ResolveRawSegment(segment, namingPolicy))));

    /// <summary>
    /// Converts a member expression to its serialized projection property path.
    /// </summary>
    /// <param name="expression">The member expression.</param>
    /// <param name="namingPolicy">The serialization naming policy.</param>
    /// <returns>The serialized property path.</returns>
    public static PropertyPath FromExpression(LambdaExpression expression, INamingPolicy namingPolicy) =>
        ResolveExpression(expression, namingPolicy, out _);

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
        var path = ResolveExpression(expression, namingPolicy, out var isMemberPath);
        if (!isMemberPath)
        {
            throw new InvalidPropertyExpression(description, expression);
        }

        return path;
    }

    static PropertyPath ResolveExpression(LambdaExpression expression, INamingPolicy namingPolicy, out bool isMemberPath)
    {
        var current = (Expression?)UnwrapConversion(expression.Body);
        var segments = new List<string>();
        if (current is MethodCallExpression methodCall &&
            methodCall.Arguments.Count == 1 &&
            methodCall.Method.IsStatic &&
            methodCall.Method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), inherit: false) &&
            DerivedPropertyFunctions.TryGet(methodCall.Method.Name, out var function))
        {
            segments.Add($"{function.Name}()");
            current = UnwrapConversion(methodCall.Arguments[0]);
        }

        while (current is MemberExpression member)
        {
            // Name CLR members before constructing a PropertyPath: a bare CLR name like Week
            // would otherwise be parsed as a derived function instead of a property.
            segments.Insert(0, namingPolicy.GetPropertyName(member.Member.Name));
            current = member.Expression;
        }

        isMemberPath = segments.Count > 0 && current is ParameterExpression;
        return new PropertyPath(string.Join('.', segments));
    }

    static Expression UnwrapConversion(Expression expression) =>
        expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary ? unary.Operand : expression;

    static string ResolveRawSegment(string segment, INamingPolicy namingPolicy) => segment switch
    {
        PropertyPath.ThisAccessorValue => segment,
        _ when segment.EndsWith("()", StringComparison.Ordinal) => segment,
        _ when segment.StartsWith('[') && segment.EndsWith(']') => $"[{namingPolicy.GetPropertyName(segment[1..^1])}]",
        _ => namingPolicy.GetPropertyName(segment)
    };

    static string ResolveSegment(IPropertyPathSegment segment, INamingPolicy namingPolicy) => segment switch
    {
        PropertyName => namingPolicy.GetPropertyName(segment.Value),
        ArrayProperty => $"[{namingPolicy.GetPropertyName(segment.Value)}]",
        DerivedPropertyFunctionSegment => $"{segment.Value}()",
        _ => segment.ToString()!
    };
}
