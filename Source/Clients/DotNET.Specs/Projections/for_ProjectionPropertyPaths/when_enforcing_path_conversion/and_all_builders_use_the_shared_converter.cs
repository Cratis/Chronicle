// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Properties;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_enforcing_path_conversion;

public class and_all_builders_use_the_shared_converter : Specification
{
    static readonly HashSet<string> _pathNamingMethods = new(StringComparer.Ordinal) { "GetPropertyName", "GetPropertyPath", "TryGetPropertyPath" };

    string[] _sources;
    string[] _offenders;

    void Establish() => _sources = GetType().Assembly.GetManifestResourceNames()
        .Where(name => name.StartsWith("ProjectionPaths.", StringComparison.Ordinal))
        .ToArray();

    void Because() => _offenders = _sources
        .Where(name => !name.EndsWith("ProjectionPropertyPaths.cs", StringComparison.Ordinal) && BypassesPathConversion(name))
        .ToArray();

    [Fact] void should_include_fluent_builders() => _sources.Any(name => name.EndsWith("KeyBuilder.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_include_model_bound_builders() => _sources.Any(name => name.EndsWith("ModelBoundProjectionBuilder.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_only_name_paths_in_the_shared_converter() => _offenders.ShouldBeEmpty();

    static bool BypassesPathConversion(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var syntax = CSharpSyntaxTree.ParseText(reader.ReadToEnd()).GetRoot();

        return syntax.DescendantNodes().Any(node => node switch
        {
            InvocationExpressionSyntax invocation => InvokedName(invocation.Expression) is { } name && _pathNamingMethods.Contains(name),
            ObjectCreationExpressionSyntax creation => IsPropertyPath(creation.Type) && !IsDynamicTargetComposition(resourceName, creation),
            CastExpressionSyntax cast => IsPropertyPath(cast.Type),
            _ => false
        });
    }

    static bool IsPropertyPath(TypeSyntax type) => type switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText == nameof(PropertyPath),
        QualifiedNameSyntax qualified => IsPropertyPath(qualified.Right),
        AliasQualifiedNameSyntax alias => IsPropertyPath(alias.Name),
        _ => false
    };

    /// <summary>
    /// FromAllBuilder combines paths already named by the shared converter with the well-known
    /// $eventContext marker. This is dynamic target syntax, not premature parsing of a CLR name.
    /// </summary>
    /// <param name="resourceName">The embedded source file the creation was found in.</param>
    /// <param name="creation">The <see cref="ObjectCreationExpressionSyntax"/> to check.</param>
    /// <returns>True when the creation is the allowed dynamic target composition.</returns>
    static bool IsDynamicTargetComposition(string resourceName, ObjectCreationExpressionSyntax creation) =>
        resourceName.EndsWith("FromAllBuilder.cs", StringComparison.Ordinal) &&
        creation.Parent is ReturnStatementSyntax &&
        creation.Ancestors().OfType<MethodDeclarationSyntax>().Any(method => method.Identifier.ValueText == "BuildDynamicTargetProperty") &&
        creation.ArgumentList is { Arguments.Count: 1 } arguments && arguments.Arguments[0].Expression is InterpolatedStringExpressionSyntax;

    static string? InvokedName(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
        MemberBindingExpressionSyntax memberBinding => memberBinding.Name.Identifier.ValueText,
        SimpleNameSyntax simpleName => simpleName.Identifier.ValueText,
        _ => null
    };
}
