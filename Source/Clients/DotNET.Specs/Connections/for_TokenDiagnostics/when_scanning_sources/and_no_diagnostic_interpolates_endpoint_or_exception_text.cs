// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.Connections.for_TokenDiagnostics.when_scanning_sources;

/// <summary>
/// Convention: client connection and token diagnostics are worded through <see cref="TokenDiagnostics"/> fixed labels.
/// Log and exception text never interpolates an endpoint, a URI part or an exception message.
/// </summary>
public class and_no_diagnostic_interpolates_endpoint_or_exception_text : Specification
{
    static readonly HashSet<string> _uriAndMessageMembers = new(StringComparer.Ordinal)
    {
        "Message", "AbsoluteUri", "OriginalString", "Authority", "Host", "DnsSafeHost", "Port", "AbsolutePath", "PathAndQuery", "Query", "UserInfo"
    };

    static readonly string[] _endpointNames = ["endpoint", "authority", "uri", "url"];

    string[] _sources;
    string[] _offenders;

    void Establish() => _sources = GetType().Assembly.GetManifestResourceNames()
        .Where(name => name.StartsWith("ClientDiagnostics.", StringComparison.Ordinal))
        .ToArray();

    void Because() => _offenders = _sources
        .Where(name => !name.EndsWith("TokenDiagnostics.cs", StringComparison.Ordinal))
        .SelectMany(Offences)
        .ToArray();

    [Fact] void should_scan_the_connection_sources() => _sources.Any(name => name.EndsWith("ExternalAuthorityTokenProvider.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_scan_the_token_provider_factory() => _sources.Any(name => name.EndsWith("TokenProviderFactory.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_interpolate_endpoint_or_exception_text() => _offenders.ShouldBeEmpty();

    static IEnumerable<string> Offences(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var tree = CSharpSyntaxTree.ParseText(reader.ReadToEnd());
        return tree.GetRoot().DescendantNodes().OfType<InterpolatedStringExpressionSyntax>()
            .Where(text => IsDiagnosticText(text) && text.Contents.OfType<InterpolationSyntax>().Any(IsEndpointOrExceptionText))
            .Select(text => $"{resourceName}:{tree.GetLineSpan(text.Span).StartLinePosition.Line + 1}");
    }

    static bool IsDiagnosticText(SyntaxNode text) => text.Ancestors().Any(ancestor => ancestor switch
    {
        ObjectCreationExpressionSyntax creation => EndsWithException(creation.Type),
        ImplicitObjectCreationExpressionSyntax => false,
        PrimaryConstructorBaseTypeSyntax baseType => EndsWithException(baseType.Type),
        ConstructorInitializerSyntax initializer => initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword),
        InvocationExpressionSyntax invocation => IsLogInvocation(invocation),
        _ => false
    });

    static bool EndsWithException(TypeSyntax type) => type.ToString().EndsWith("Exception", StringComparison.Ordinal);

    static bool IsLogInvocation(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText.StartsWith("Log", StringComparison.Ordinal),
        IdentifierNameSyntax name => name.Identifier.ValueText.StartsWith("Log", StringComparison.Ordinal),
        _ => false
    };

    static bool IsEndpointOrExceptionText(InterpolationSyntax interpolation) =>
        interpolation.Expression.DescendantNodesAndSelf().Any(node => node switch
        {
            MemberAccessExpressionSyntax member => _uriAndMessageMembers.Contains(member.Name.Identifier.ValueText),
            IdentifierNameSyntax name => _endpointNames.Any(endpoint => name.Identifier.ValueText.Contains(endpoint, StringComparison.OrdinalIgnoreCase)),
            _ => false
        });
}
