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

    const string LoggingCanary = "class C { void M() { _logger.LogWarning($\"{ex.Message}\"); } }";
    const string LoggerMessageCanary = "class C { void M() { _logger.RequestingAccessToken($\"{tokenEndpoint}\"); } }";
    const string ExceptionCanary = "class C { void M() { throw new InsecureTokenEndpoint($\"{endpoint}\"); } }";
    const string HelperCanary = "class C { void M() { TokenDiagnostics.Describe($\"{endpoint}\"); } }";
    const string CleanCanary = "class C { void M() { _logger.RequestingAccessToken(TokenDiagnostics.Label(source)); } }";

    string[] _sources;
    string[] _offenders;
    string[] _canaryOffenders;
    Sinks _sinks;

    void Establish()
    {
        _sources = GetType().Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("ClientDiagnostics.", StringComparison.Ordinal))
            .ToArray();
        _sinks = Sinks.From(_sources.Select(ParseResource));
    }

    void Because()
    {
        _offenders = _sources
            .Where(name => !name.EndsWith("TokenDiagnostics.cs", StringComparison.Ordinal))
            .SelectMany(name => Offences(name, ParseResource(name), _sinks))
            .ToArray();
        _canaryOffenders = new[] { LoggingCanary, LoggerMessageCanary, ExceptionCanary, HelperCanary, CleanCanary }
            .Select(code => Offences("canary", CSharpSyntaxTree.ParseText(code), _sinks).Count().ToString())
            .ToArray();
    }

    [Fact] void should_scan_the_connection_sources() => _sources.Any(name => name.EndsWith("ExternalAuthorityTokenProvider.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_scan_the_token_provider_factory() => _sources.Any(name => name.EndsWith("TokenProviderFactory.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_know_the_generated_logger_message_methods() => _sinks.LoggerMessageMethods.ShouldContain("RequestingAccessToken");
    [Fact] void should_know_the_custom_exception_types() => _sinks.ExceptionTypes.ShouldContain("InsecureTokenEndpoint");
    [Fact] void should_flag_an_interpolated_exception_message_in_a_log_call() => _canaryOffenders[0].ShouldEqual("1");
    [Fact] void should_flag_an_interpolated_endpoint_in_a_generated_logger_message_call() => _canaryOffenders[1].ShouldEqual("1");
    [Fact] void should_flag_an_interpolated_endpoint_in_a_custom_exception() => _canaryOffenders[2].ShouldEqual("1");
    [Fact] void should_flag_an_interpolated_endpoint_in_a_diagnostic_helper_call() => _canaryOffenders[3].ShouldEqual("1");
    [Fact] void should_not_flag_a_fixed_label() => _canaryOffenders[4].ShouldEqual("0");
    [Fact] void should_not_interpolate_endpoint_or_exception_text() => _offenders.ShouldBeEmpty();

    static SyntaxTree ParseResource(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return CSharpSyntaxTree.ParseText(reader.ReadToEnd());
    }

    static IEnumerable<string> Offences(string name, SyntaxTree tree, Sinks sinks) =>
        tree.GetRoot().DescendantNodes().OfType<InterpolatedStringExpressionSyntax>()
            .Where(text => IsDiagnosticText(text, sinks) && text.Contents.OfType<InterpolationSyntax>().Any(IsEndpointOrExceptionText))
            .Select(text => $"{name}:{tree.GetLineSpan(text.Span).StartLinePosition.Line + 1}");

    static bool IsDiagnosticText(SyntaxNode text, Sinks sinks) => text.Ancestors().Any(ancestor => ancestor switch
    {
        ObjectCreationExpressionSyntax creation => sinks.IsException(creation.Type),
        ImplicitObjectCreationExpressionSyntax => false,
        PrimaryConstructorBaseTypeSyntax baseType => sinks.IsException(baseType.Type),
        ConstructorInitializerSyntax initializer => initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword),
        InvocationExpressionSyntax invocation => sinks.IsSinkInvocation(invocation),
        _ => false
    });

    static bool IsEndpointOrExceptionText(InterpolationSyntax interpolation) =>
        interpolation.Expression.DescendantNodesAndSelf().Any(node => node switch
        {
            MemberAccessExpressionSyntax member => _uriAndMessageMembers.Contains(member.Name.Identifier.ValueText),
            IdentifierNameSyntax name => _endpointNames.Any(endpoint => name.Identifier.ValueText.Contains(endpoint, StringComparison.OrdinalIgnoreCase)),
            _ => false
        });

    sealed class Sinks(HashSet<string> loggerMessageMethods, HashSet<string> exceptionTypes)
    {
        public HashSet<string> LoggerMessageMethods { get; } = loggerMessageMethods;

        public HashSet<string> ExceptionTypes { get; } = exceptionTypes;

        public static Sinks From(IEnumerable<SyntaxTree> trees)
        {
            var roots = trees.Select(tree => tree.GetRoot()).ToArray();
            var methods = roots
                .SelectMany(root => root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                .Where(method => method.Modifiers.Any(SyntaxKind.PartialKeyword))
                .Select(method => method.Identifier.ValueText)
                .ToHashSet(StringComparer.Ordinal);

            var derived = roots
                .SelectMany(root => root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                .Where(type => type.BaseList is not null)
                .Select(type => (Name: type.Identifier.ValueText, Bases: type.BaseList!.Types.Select(_ => Simple(_.Type)).ToArray()))
                .ToArray();
            var exceptions = new HashSet<string>(StringComparer.Ordinal);
            bool grew;
            do
            {
                grew = false;
                foreach (var (name, _) in derived.Where(_ => _.Bases.Any(@base => IsExceptionName(@base) || exceptions.Contains(@base))))
                {
                    grew |= exceptions.Add(name);
                }
            }
            while (grew);

            return new Sinks(methods, exceptions);
        }

        public bool IsException(TypeSyntax type)
        {
            var name = Simple(type);
            return IsExceptionName(name) || ExceptionTypes.Contains(name);
        }

        public bool IsSinkInvocation(InvocationExpressionSyntax invocation) => invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => IsSinkName(member.Name.Identifier.ValueText) || IsDiagnosticHelper(member),
            IdentifierNameSyntax name => IsSinkName(name.Identifier.ValueText),
            _ => false
        };

        static bool IsExceptionName(string name) => name.EndsWith("Exception", StringComparison.Ordinal);

        static string Simple(TypeSyntax type) => type switch
        {
            QualifiedNameSyntax qualified => Simple(qualified.Right),
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => type.ToString()
        };

        static bool IsDiagnosticHelper(MemberAccessExpressionSyntax member) =>
            member.Expression is IdentifierNameSyntax name && string.Equals(name.Identifier.ValueText, "TokenDiagnostics", StringComparison.Ordinal);

        bool IsSinkName(string name) => name.StartsWith("Log", StringComparison.Ordinal) || LoggerMessageMethods.Contains(name);
    }
}
