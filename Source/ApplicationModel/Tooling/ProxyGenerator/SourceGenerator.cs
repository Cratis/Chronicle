// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Aksio.Cratis.Applications.ProxyGenerator.Syntax;
using Aksio.Cratis.Applications.ProxyGenerator.Templates;
using HandlebarsDotNet;
using Microsoft.CodeAnalysis;

namespace Aksio.Cratis.Applications.ProxyGenerator;

/// <summary>
/// /// Represents a <see cref="ISourceGenerator"/> for generating proxies for frontend use.
/// </summary>
[Generator]
public class SourceGenerator : ISourceGenerator
{
    static readonly Regex _routeRegex = new(@"(\{[\w]*\})", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
    static readonly Dictionary<string, int> _fileHashes = new();

    /// <inheritdoc/>
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    /// <inheritdoc/>
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.Compilation.GetDiagnostics().Any(_ => _.Severity == DiagnosticSeverity.Error)) return;

        var receiver = context.SyntaxReceiver as SyntaxReceiver;
        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.rootnamespace", out var rootNamespace);
        if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.aksioproxyoutput", out var outputFolder))
        {
            context.ReportDiagnostic(Diagnostics.MissingOutputPath);
            return;
        }
        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.aksiouserouteaspath", out var useRouteAsPathAsString);

        var useRouteAsPath = !string.IsNullOrEmpty(useRouteAsPathAsString);

        foreach (var classDeclaration in receiver!.Candidates)
        {
            try
            {
                var model = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree, true);
                if (!(model.GetDeclaredSymbol(classDeclaration) is ITypeSymbol type)) continue;

                if (string.IsNullOrEmpty(rootNamespace))
                {
                    rootNamespace = type.ContainingAssembly.Name!;
                }

                var routeAttribute = type.GetRouteAttribute();
                if (routeAttribute == default) return;

                var baseApiRoute = routeAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;

                var publicInstanceMethods = type.GetPublicInstanceMethodsFrom();

                OutputCommands(type, publicInstanceMethods, baseApiRoute, rootNamespace!, outputFolder, useRouteAsPath);
                OutputQueries(context, type, publicInstanceMethods, baseApiRoute, rootNamespace!, outputFolder, useRouteAsPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errors {ex}");
            }
        }
    }

    static void OutputCommands(ITypeSymbol type, IEnumerable<IMethodSymbol> methods, string baseApiRoute, string rootNamespace, string outputFolder, bool useRouteAsPath)
    {
        var targetFolder = GetTargetFolder(type, rootNamespace, outputFolder, useRouteAsPath, baseApiRoute);
        foreach (var commandMethod in methods.Where(_ => _.GetAttributes().Any(_ => _.IsHttpPostAttribute())))
        {
            var route = GetRoute(baseApiRoute, commandMethod);

            var properties = new List<PropertyDescriptor>();
            var importStatements = new HashSet<ImportStatement>();
            foreach (var parameter in commandMethod.Parameters)
            {
                if (parameter.Type.IsKnownType())
                {
                    properties.Add(new(
                        parameter.Name,
                        parameter.Type.GetTypeScriptType(out var additionalImportStatements),
                        parameter.Type.IsEnumerable()
                    ));
                    additionalImportStatements.ForEach(_ => importStatements.Add(_));
                }
                else
                {
                    properties.AddRange(parameter.Type.GetPropertyDescriptorsFrom(out var additionalImportStatements));
                    additionalImportStatements.ForEach(_ => importStatements.Add(_));
                }
            }

            var requestArguments = GetRequestArgumentsFrom(commandMethod, ref route, importStatements);

            var typeName = commandMethod.Name;
            var commandDescriptor = new CommandDescriptor(route, typeName, properties, importStatements, requestArguments);
            var renderedTemplate = TemplateTypes.Command(commandDescriptor);
            if (renderedTemplate != default)
            {
                var file = Path.Combine(targetFolder, $"{typeName}.ts");
                WriteFile(file, renderedTemplate);
            }
        }
    }

    static void OutputQueries(GeneratorExecutionContext context, ITypeSymbol type, IEnumerable<IMethodSymbol> methods, string baseApiRoute, string rootNamespace, string outputFolder, bool useRouteAsPath)
    {
        var targetFolder = GetTargetFolder(type, rootNamespace, outputFolder, useRouteAsPath, baseApiRoute);
        foreach (var queryMethod in methods.Where(_ => _.GetAttributes().Any(_ => _.IsHttpGetAttribute())))
        {
            var modelType = queryMethod.ReturnType;

            if (modelType is INamedTypeSymbol modelTypeAsNamedType)
            {
                if (modelType.ToString() == typeof(Task).FullName)
                {
                    continue;
                }

                if (modelTypeAsNamedType.ConstructedFrom.ToString().StartsWith(typeof(Task).FullName, StringComparison.InvariantCulture) && modelTypeAsNamedType.IsGenericType)
                {
                    modelTypeAsNamedType = (modelTypeAsNamedType.TypeArguments[0] as INamedTypeSymbol)!;
                }
                var route = GetRoute(baseApiRoute, queryMethod);
                var importStatements = new HashSet<ImportStatement>();

                var actualType = modelTypeAsNamedType;
                var isEnumerable = false;
                if (actualType.IsObservableClient())
                {
                    actualType = (actualType.TypeArguments[0] as INamedTypeSymbol)!;
                }

                if (actualType.IsEnumerable())
                {
                    if (!actualType.IsGenericType)
                    {
                        context.ReportDiagnostic(Diagnostics.UnableToResolveModelType($"{type.ToDisplayString()}:{queryMethod.Name}"));
                        return;
                    }

                    actualType = (actualType.TypeArguments[0] as INamedTypeSymbol)!;
                    isEnumerable = true;
                }

                var targetFile = Path.Combine(targetFolder, $"{queryMethod.Name}.ts");
                OutputType(actualType, rootNamespace, outputFolder, targetFile, importStatements, useRouteAsPath, baseApiRoute);

                var queryArguments = GetRequestArgumentsFrom(queryMethod, ref route, importStatements);

                var typeName = actualType.IsKnownType() ? actualType.GetTypeScriptType(out _) : actualType.Name;
                var queryDescriptor = new QueryDescriptor(route, queryMethod.Name, typeName, isEnumerable, importStatements, queryArguments);
                var renderedTemplate = modelTypeAsNamedType.IsObservableClient() ?
                    TemplateTypes.ObservableQuery(queryDescriptor) :
                    TemplateTypes.Query(queryDescriptor);
                if (renderedTemplate != default)
                {
                    WriteFile(targetFile, renderedTemplate);
                }
            }
        }
    }

    static List<RequestArgumentDescriptor> GetRequestArgumentsFrom(IMethodSymbol queryMethod, ref string route, HashSet<ImportStatement> importStatements)
    {
        var queryArguments = new List<RequestArgumentDescriptor>();
        if (queryMethod.Parameters.Length > 0)
        {
            foreach (var parameter in queryMethod.Parameters)
            {
                var isArgument = false;
                var attributes = parameter.GetAttributes();
                if (attributes.Any(_ => _.IsFromRouteAttribute()))
                {
                    route = route.Replace($"{{{parameter.Name}}}", $"{{{{{parameter.Name}}}}}");
                    isArgument = true;
                }
                if (attributes.Any(_ => _.IsFromQueryAttribute()))
                {
                    if (!route.Contains('?'))
                    {
                        route = $"{route}?";
                    }
                    else
                    {
                        route = $"{route}&";
                    }

                    var isNullable = parameter.NullableAnnotation == NullableAnnotation.Annotated;
                    route = $"{route}{parameter.Name}={{{{{parameter.Name}}}}}";
                    isArgument = true;
                }

                if (isArgument)
                {
                    queryArguments.Add(
                        new(
                            parameter.Name,
                            parameter.Type.GetTypeScriptType(out var additionalImportStatements),
                            parameter.NullableAnnotation == NullableAnnotation.Annotated));

                    additionalImportStatements.ForEach(_ => importStatements.Add(_));
                }
            }
        }

        return queryArguments;
    }

    static void OutputType(ITypeSymbol type, string rootNamespace, string outputFolder, string parentFile, HashSet<ImportStatement> parentImportStatements, bool useRouteAsPath, string baseApiRoute)
    {
        if (type.IsKnownType()) return;

        var targetFolder = GetTargetFolder(type, rootNamespace, outputFolder, useRouteAsPath, baseApiRoute);
        var targetFile = Path.Combine(targetFolder, $"{type.Name}.ts");
        var relativeImport = new Uri(parentFile).MakeRelativeUri(new Uri(targetFile));
        var importPath = Path.GetFileNameWithoutExtension(relativeImport.ToString());
        if (Path.GetDirectoryName(targetFile) == Path.GetDirectoryName(parentFile)) importPath = $"./{importPath}";
        parentImportStatements.Add(new ImportStatement(type.Name, importPath));

        var properties = type.GetMembers().Where(_ => !_.IsStatic
            && _ is IPropertySymbol propertySymbol
            && propertySymbol.DeclaredAccessibility == Accessibility.Public).Cast<IPropertySymbol>();

        var typeImportStatements = new HashSet<ImportStatement>();
        var propertyDescriptors = new List<PropertyDescriptor>();

        foreach (var property in properties)
        {
            var targetType = property.Type.GetTypeScriptType(out var additionalImportStatements);
            additionalImportStatements.ForEach(_ => typeImportStatements.Add(_));
            var isEnumerable = property.Type.IsEnumerable();
            if (targetType == TypeSymbolExtensions.AnyType)
            {
                var actualType = property.Type;
                if (isEnumerable)
                {
                    actualType = ((INamedTypeSymbol)property.Type).TypeArguments[0];
                }
                OutputType(actualType, rootNamespace, outputFolder, targetFile, typeImportStatements, useRouteAsPath, baseApiRoute);

                propertyDescriptors.Add(new PropertyDescriptor(property.Name, actualType.Name, isEnumerable));
            }
            else
            {
                propertyDescriptors.Add(new PropertyDescriptor(property.Name, targetType, isEnumerable));
            }
        }

        var typeDescriptor = new TypeDescriptor(type.Name, propertyDescriptors, typeImportStatements);
        var renderedTemplate = TemplateTypes.Type(typeDescriptor);
        if (renderedTemplate != default)
        {
            Directory.CreateDirectory(targetFolder);
            WriteFile(targetFile, renderedTemplate);
        }
    }

    static string GetRoute(string baseApiRoute, IMethodSymbol commandMethod)
    {
        var methodRoute = commandMethod.GetMethodRoute();
        var fullRoute = baseApiRoute;
        if (methodRoute.Length > 0)
        {
            fullRoute = $"{baseApiRoute}/{methodRoute}";
        }

        return fullRoute;
    }

    static string GetTargetFolder(ITypeSymbol type, string rootNamespace, string outputFolder, bool useRouteAsPath, string baseApiRoute)
    {
        var relativePath = string.Empty;

        if (useRouteAsPath)
        {
            baseApiRoute = _routeRegex.Replace(baseApiRoute, string.Empty);
            baseApiRoute = baseApiRoute.Replace("//", "/");
            const string apiPrefix = "/api";
            if (baseApiRoute.StartsWith(apiPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                baseApiRoute = baseApiRoute.Substring(apiPrefix.Length);
            }
            if (baseApiRoute.StartsWith("/", StringComparison.InvariantCulture)) baseApiRoute = baseApiRoute.Substring(1);

            relativePath = baseApiRoute.Replace('/', Path.DirectorySeparatorChar);
        }
        else
        {
            var segments = type.ContainingNamespace.ToDisplayString().Replace(rootNamespace, string.Empty)
                                                            .Split('.')
                                                            .Where(_ => _.Length > 0);

            relativePath = string.Join(Path.DirectorySeparatorChar.ToString(), segments);
        }

        var folder = Path.Combine(outputFolder, relativePath);

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        return folder;
    }

    static void WriteFile(string file, string content)
    {
        if (string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(file))) return;
        var hashCode = StringComparer.InvariantCulture.GetHashCode(content);
        if (_fileHashes.ContainsKey(file) && _fileHashes[file] == hashCode) return;
        _fileHashes[file] = hashCode;
        File.WriteAllText(file, content);
    }
}
