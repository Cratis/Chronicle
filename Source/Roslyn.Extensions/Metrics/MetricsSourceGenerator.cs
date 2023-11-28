using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Extensions.Templates;

namespace Roslyn.Extensions.Metrics;

[Generator]
public class MetricsSourceGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not MetricsSyntaxReceiver receiver) return;

        var counterAttribute = context.Compilation.GetTypeByMetadataName("Fundamentals.Metrics.CounterAttribute`1");
        foreach (var candidate in receiver.Candidates)
        {
            var templateData = new MetricsTemplateData
            {
                Namespace = (candidate.Parent as BaseNamespaceDeclarationSyntax)!.Name.ToString(),
                ClassName = candidate.Identifier.ValueText
            };

            var semanticModel = context.Compilation.GetSemanticModel(candidate.SyntaxTree);
            foreach (var member in candidate.Members)
            {
                if (member is not MethodDeclarationSyntax method) continue;

                var methodSymbol = semanticModel.GetDeclaredSymbol(method);
                if (methodSymbol is not null)
                {
                    var attributes = methodSymbol.GetAttributes();
                    var attribute = attributes.FirstOrDefault(_ => SymbolEqualityComparer.Default.Equals(_.AttributeClass?.OriginalDefinition, counterAttribute));
                    if (attribute is not null)
                    {
                        var tags = method.ParameterList.Parameters.Select(parameter => new CounterTagTemplateData
                        {
                            Name = parameter.Identifier.ValueText,
                            Type = parameter.Type!.ToString()
                        });

                        var type = attribute.AttributeClass!.TypeArguments[0].ToString();
                        var name = attribute.ConstructorArguments[0].Value!.ToString();
                        var description = attribute.ConstructorArguments[1].Value!.ToString();
                        templateData.Counters.Add(
                                new CounterTemplateData
                                {
                                    Name = name,
                                    Description = description,
                                    Type = type,
                                    MethodName = method.Identifier.ValueText,
                                    Tags = tags
                                });
                    }
                }
            }

            if (templateData.Counters.Count > 0)
            {
                var source = TemplateTypes.Metrics(templateData);
                context.AddSource($"{candidate.Identifier.ValueText}.g.cs", source);
            }
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new MetricsSyntaxReceiver());
    }
}
