// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Resolves property names within a projection path using the serialization naming policy.
/// </summary>
internal static class ProjectionPropertyPaths
{
    /// <summary>
    /// Applies the naming policy to parsed segments according to their source or target role.
    /// </summary>
    /// <param name="path">The property path to resolve.</param>
    /// <param name="namingPolicy">The serialization naming policy.</param>
    /// <param name="role">Whether the path reads event data or addresses read-model state.</param>
    /// <returns>The serialized property path.</returns>
    public static PropertyPath Resolve(PropertyPath path, INamingPolicy namingPolicy, ProjectionPathRole role) =>
        new(string.Join('.', path.Segments.Select((segment, index) => ResolveSegment(segment, namingPolicy, role, index))));

    /// <summary>
    /// Applies the naming policy to a raw path, recognizing functions only by explicit parentheses.
    /// </summary>
    /// <param name="path">The raw property path to resolve.</param>
    /// <param name="namingPolicy">The serialization naming policy.</param>
    /// <param name="role">Whether the path reads event data or addresses read-model state.</param>
    /// <returns>The serialized property path.</returns>
    public static PropertyPath Resolve(string path, INamingPolicy namingPolicy, ProjectionPathRole role) =>
        new(string.Join('.', path.Split('.').Select((segment, index) => ResolveRawSegment(segment, namingPolicy, role, index))));

    /// <summary>
    /// Applies the naming policy to a validated canonical context path without reparsing its function calls.
    /// </summary>
    /// <param name="path">The validated canonical context property path.</param>
    /// <param name="namingPolicy">The serialization naming policy.</param>
    /// <returns>The serialized context path, preserving explicit function parentheses.</returns>
    public static string RenderContextPath(string path, INamingPolicy namingPolicy) =>
        string.Join('.', path.Split('.').Select((segment, index) => ResolveRawSegment(segment, namingPolicy, ProjectionPathRole.Source, index)));

    /// <summary>
    /// Converts a CLR member to its serialized projection property path without parsing its CLR name.
    /// </summary>
    /// <param name="property">The CLR property to resolve.</param>
    /// <param name="namingPolicy">The serialization naming policy.</param>
    /// <param name="role">Whether the path reads event data or addresses read-model state.</param>
    /// <returns>The serialized property path.</returns>
    public static PropertyPath Resolve(PropertyInfo property, INamingPolicy namingPolicy, ProjectionPathRole role) =>
        Resolve(property.Name, namingPolicy, role);

    /// <summary>
    /// Converts a member expression to its serialized projection property path.
    /// </summary>
    /// <param name="expression">The member expression.</param>
    /// <param name="namingPolicy">The serialization naming policy.</param>
    /// <param name="role">Whether the path reads event data or addresses read-model state.</param>
    /// <returns>The serialized property path.</returns>
    public static PropertyPath FromExpression(LambdaExpression expression, INamingPolicy namingPolicy, ProjectionPathRole role) =>
        ResolveExpression(expression, namingPolicy, role, out _);

    /// <summary>
    /// Converts a member expression and retains the caller's invalid-expression description.
    /// </summary>
    /// <param name="expression">The member expression.</param>
    /// <param name="namingPolicy">The serialization naming policy.</param>
    /// <param name="role">Whether the path reads event data or addresses read-model state.</param>
    /// <param name="description">The description of the property being configured.</param>
    /// <returns>The serialized property path.</returns>
    /// <exception cref="InvalidPropertyExpression">The expression is not a property path.</exception>
    public static PropertyPath FromExpression(LambdaExpression expression, INamingPolicy namingPolicy, ProjectionPathRole role, string description)
    {
        var path = ResolveExpression(expression, namingPolicy, role, out var isMemberPath);
        if (!isMemberPath)
        {
            throw new InvalidPropertyExpression(description, expression);
        }

        return path;
    }

    static PropertyPath ResolveExpression(LambdaExpression expression, INamingPolicy namingPolicy, ProjectionPathRole role, out bool isMemberPath)
    {
        var current = (Expression?)UnwrapConversion(expression.Body);
        var segments = new List<string>();
        if (GetDerivedCall(current, role) is { } derivedCall)
        {
            segments.Add($"{derivedCall.Name}()");
            current = UnwrapConversion(derivedCall.MethodCall.Arguments[0]);
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

    static (string Name, MethodCallExpression MethodCall)? GetDerivedCall(Expression? expression, ProjectionPathRole role)
    {
        if (role != ProjectionPathRole.Source)
        {
            return null;
        }

        if (expression is not MethodCallExpression methodCall)
        {
            return null;
        }

        if (methodCall.Arguments.Count != 1)
        {
            return null;
        }

        if (!methodCall.Method.IsStatic)
        {
            return null;
        }

        if (!methodCall.Method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), inherit: false))
        {
            return null;
        }

        if (UnwrapConversion(methodCall.Arguments[0]) is not MemberExpression)
        {
            return null;
        }

        if (!DerivedPropertyFunctions.TryGet(methodCall.Method.Name, out var function))
        {
            return null;
        }

        return (function.Name, methodCall);
    }

    static Expression UnwrapConversion(Expression expression) =>
        expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary ? unary.Operand : expression;

    static string ResolveRawSegment(string segment, INamingPolicy namingPolicy, ProjectionPathRole role, int index) => segment switch
    {
        PropertyPath.ThisAccessorValue => segment,
        _ when segment.EndsWith("()", StringComparison.Ordinal) => role == ProjectionPathRole.Source && index > 0
            ? segment
            : namingPolicy.GetPropertyName(segment[..^2]),
        _ when segment.StartsWith('[') && segment.EndsWith(']') => $"[{namingPolicy.GetPropertyName(segment[1..^1])}]",
        _ => namingPolicy.GetPropertyName(segment)
    };

    static string ResolveSegment(IPropertyPathSegment segment, INamingPolicy namingPolicy, ProjectionPathRole role, int index) => segment switch
    {
        PropertyName => namingPolicy.GetPropertyName(segment.Value),
        ArrayProperty => $"[{namingPolicy.GetPropertyName(segment.Value)}]",

        // A parsed source has already resolved its function syntax. It still needs a receiver;
        // a first segment named Week is a member, even if PropertyPath classified it as a function.
        DerivedPropertyFunctionSegment when role == ProjectionPathRole.Source && index > 0 => $"{segment.Value}()",
        DerivedPropertyFunctionSegment => namingPolicy.GetPropertyName(segment.Value),
        _ => segment.ToString()!
    };
}
