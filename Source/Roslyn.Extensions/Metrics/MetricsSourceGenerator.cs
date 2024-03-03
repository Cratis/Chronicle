// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Extensions.Templates;

namespace Roslyn.Extensions.Metrics;

/// <summary>
/// Represents the source generator for metrics.
/// </summary>
[Generator]
public class MetricsSourceGenerator : ISourceGenerator
{
    static readonly string[] _systemUsings = new[]
    {
        "System.Diagnostics",
        "System.Diagnostics.Metrics"
    };

    /// <inheritdoc/>
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not MetricsSyntaxReceiver receiver) return;

        var counterAttribute = context.Compilation.GetTypeByMetadataName("Cratis.Metrics.CounterAttribute`1")!;
        var measurementAttribute = context.Compilation.GetTypeByMetadataName("Cratis.Metrics.MeasurementAttribute`1")!;
        foreach (var candidate in receiver.Candidates)
        {
            var classDefinition = $"{candidate.Modifiers} class {candidate.Identifier.ValueText}";
            var usings = GetUsingsFor(candidate);

            var templateData = new MetricsTemplateData
            {
                Namespace = (candidate.Parent as BaseNamespaceDeclarationSyntax)!.Name.ToString(),
                ClassName = candidate.Identifier.ValueText,
                ClassDefinition = classDefinition,
                UsingStatements = usings.ToList()
            };

            var semanticModel = context.Compilation.GetSemanticModel(candidate.SyntaxTree);
            foreach (var member in candidate.Members)
            {
                if (member is not MethodDeclarationSyntax method) continue;

                var methodSymbol = semanticModel.GetDeclaredSymbol(method);
                if (methodSymbol is not null)
                {
                    var attributes = methodSymbol.GetAttributes();
                    var tags = GetParametersAsTags(method);
                    var methodSignature = $"{method.Modifiers} {method.ReturnType} {method.Identifier.ValueText}({method.ParameterList.Parameters})";

                    var isScoped = false;
                    var scopeParameter = string.Empty;

                    if (method.ParameterList.Parameters.Count > 0)
                    {
                        var type = method.ParameterList.Parameters[0].Type;
                        if (type is GenericNameSyntax genericNameSyntax && genericNameSyntax.Identifier.ValueText == "IMeterScope")
                        {
                            isScoped = true;
                            scopeParameter = method.ParameterList.Parameters[0].Identifier.ValueText;

                            tags = tags.Skip(1);
                        }
                    }
                    AddMetricIfAny(templateData.Counters, counterAttribute, method, methodSignature, attributes, isScoped, scopeParameter, tags);
                    AddMetricIfAny(templateData.Measurements, measurementAttribute, method, methodSignature, attributes, isScoped, scopeParameter, tags);
                }
            }

            if (templateData.Counters.Count > 0)
            {
                var source = TemplateTypes.Metrics(templateData);
                context.AddSource($"{candidate.Identifier.ValueText}.g.cs", source);
            }
        }
    }

    /// <inheritdoc/>
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new MetricsSyntaxReceiver());
    }

    static IEnumerable<string> GetUsingsFor(ClassDeclarationSyntax candidate)
    {
        var current = candidate.Parent;

        if (current is null)
        {
            return Enumerable.Empty<string>();
        }

        var usings = new List<string>();

        for (; ; )
        {
            usings.AddRange(current.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(_ => _.Name.ToString()));
            if (current is CompilationUnitSyntax)
            {
                break;
            }
            current = current.Parent;
            if (current is null)
            {
                break;
            }
        }

        foreach (var usingNamespace in _systemUsings.Reverse())
        {
            if (!usings.Contains(usingNamespace))
            {
                usings.Insert(0, usingNamespace);
            }
        }

        return usings.Distinct();
    }

    static IEnumerable<TagTemplateData> GetParametersAsTags(MethodDeclarationSyntax method) =>
     method.ParameterList.Parameters.Select(parameter => new TagTemplateData
     {
         Name = parameter.Identifier.ValueText,
         Type = parameter.Type!.ToString()
     });

    void AddMetricIfAny(
        IList<MetricTemplateData> metrics,
        INamedTypeSymbol attributeToLookFor,
        MethodDeclarationSyntax method,
        string methodSignature,
        ImmutableArray<AttributeData> attributes,
        bool isScoped,
        string scopeParameter,
        IEnumerable<TagTemplateData> tags)
    {
        var attribute = attributes.FirstOrDefault(_ => SymbolEqualityComparer.Default.Equals(_.AttributeClass?.OriginalDefinition, attributeToLookFor));
        if (attribute is not null)
        {
            var type = attribute.AttributeClass!.TypeArguments[0].ToString();
            var name = attribute.ConstructorArguments[0].Value!.ToString();
            var description = attribute.ConstructorArguments[1].Value!.ToString();
            metrics.Add(
                    new MetricTemplateData
                    {
                        Name = name,
                        Description = description,
                        Type = type,
                        MethodName = method.Identifier.ValueText,
                        MethodSignature = methodSignature,
                        IsScoped = isScoped,
                        ScopeParameter = scopeParameter,
                        Tags = tags
                    });
        }
    }
}
