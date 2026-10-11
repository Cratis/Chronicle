// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
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

        return syntax.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(invocation => InvokedName(invocation.Expression))
            .Any(name => name is not null && _pathNamingMethods.Contains(name));
    }

    static string? InvokedName(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
        MemberBindingExpressionSyntax memberBinding => memberBinding.Name.Identifier.ValueText,
        SimpleNameSyntax simpleName => simpleName.Identifier.ValueText,
        _ => null
    };
}
