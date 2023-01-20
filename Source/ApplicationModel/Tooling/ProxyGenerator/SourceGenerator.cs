// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Aksio.Cratis.Applications.ProxyGenerator.Syntax;
using Aksio.Cratis.Applications.ProxyGenerator.Templates;
using HandlebarsDotNet;
using Microsoft.CodeAnalysis;

namespace Aksio.Cratis.Applications.ProxyGenerator;

/// <summary>
/// Represents a <see cref="ISourceGenerator"/> for generating proxies for frontend use.
/// </summary>
[Generator]
public class SourceGenerator : ISourceGenerator
{
    static readonly string[] _allowedCompilerErrors = new[]
    {
        "CS8795"
    };

    static readonly Regex _routeRegex = new(@"(\{[\w]*\})", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
    static readonly Dictionary<string, int> _fileHashes = new();
    static readonly List<ITypeSymbol> _derivedTypes = new();

    /// <inheritdoc/>
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    /// <inheritdoc/>
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.Compilation.GetDiagnostics().Any(_ => _.Severity == DiagnosticSeverity.Error && !_allowedCompilerErrors.Any(e => _.Id == e))) return;

        var receiver = context.SyntaxReceiver as SyntaxReceiver;
        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.rootnamespace", out var rootNamespace);
        if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.aksioproxyoutput", out var outputFolder))
        {
            context.ReportDiagnostic(Diagnostics.MissingOutputPath);
            return;
        }
        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.aksiouserouteaspath", out var useRouteAsPathAsString);

        var useRouteAsPath = !string.IsNullOrEmpty(useRouteAsPathAsString);
        foreach (var derivedType in receiver!.DerivedTypes)
        {
            var model = context.Compilation.GetSemanticModel(derivedType.SyntaxTree, true);
            if (model.GetDeclaredSymbol(derivedType) is not ITypeSymbol type) continue;
            _derivedTypes.Add(type);
        }

        foreach (var classDeclaration in receiver!.Candidates)
        {
            try
            {
                var model = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree, true);
                if (model.GetDeclaredSymbol(classDeclaration) is not ITypeSymbol type) continue;

                if (string.IsNullOrEmpty(rootNamespace))
                {
                    rootNamespace = type.ContainingAssembly.Name!;
                }

                var routeAttribute = type.GetRouteAttribute();
                if (routeAttribute == default) return;

                var baseApiRoute = routeAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;

                var publicInstanceMethods = type.GetPublicInstanceMethodsFrom();

                OutputCommands(context, type, publicInstanceMethods, baseApiRoute, rootNamespace!, outputFolder, useRouteAsPath);
                OutputQueries(context, type, publicInstanceMethods, baseApiRoute, rootNamespace!, outputFolder, useRouteAsPath);
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostics.UnknownError(ex));
            }
        }
    }

    static void OutputCommands(GeneratorExecutionContext context, ITypeSymbol type, IEnumerable<IMethodSymbol> methods, string baseApiRoute, string rootNamespace, string outputFolder, bool useRouteAsPath)
    {
        var targetFolder = GetTargetFolder(type, rootNamespace, outputFolder, useRouteAsPath, baseApiRoute);
        foreach (var commandMethod in methods.Where(_ => _.GetAttributes().Any(_ => _.IsHttpPostAttribute())))
        {
            var route = GetRoute(baseApiRoute, commandMethod);
            var properties = new List<PropertyDescriptor>();
            var importStatements = new HashSet<ImportStatement>();

            var typeName = commandMethod.Name;
            var targetFile = Path.Combine(targetFolder, $"{typeName}.ts");
            targetFile = Path.GetFullPath(targetFile);

            foreach (var parameter in commandMethod.Parameters)
            {
                var isNullable = parameter.Type.NullableAnnotation == NullableAnnotation.Annotated;
                if (parameter.Type.IsKnownType())
                {
                    var targetType = parameter.Type.GetTypeScriptType(out var additionalImportStatements);
                    properties.Add(new(
                        parameter.Name,
                        targetType.Type,
                        targetType.Constructor,
                        parameter.Type.IsEnumerable(),
                        isNullable,
                        false
                    ));
                    additionalImportStatements.ForEach(_ => importStatements.Add(_));
                }
                else
                {
                    var publicProperties = parameter.Type.GetPublicPropertiesFrom();
                    properties.AddRange(GetPropertyDescriptorsAndOutputComplexTypes(
                        context,
                        rootNamespace,
                        outputFolder,
                        useRouteAsPath,
                        baseApiRoute,
                        targetFile,
                        publicProperties,
                        importStatements));
                }
            }

            var requestArguments = GetRequestArgumentsFrom(commandMethod, ref route, importStatements);
            var modelDescriptor = GetModelDescriptorFor(
                context,
                commandMethod,
                type,
                commandMethod.ReturnType,
                baseApiRoute,
                rootNamespace,
                outputFolder,
                useRouteAsPath,
                targetFile);

            modelDescriptor.Imports.ForEach(_ => importStatements.Add(_));

            var commandDescriptor = new CommandDescriptor(
                route,
                typeName,
                properties,
                importStatements,
                requestArguments,
                modelDescriptor != ModelDescriptor.Empty,
                modelDescriptor);

            var renderedTemplate = TemplateTypes.Command(commandDescriptor);
            if (renderedTemplate != default)
            {
                WriteFile(targetFile, renderedTemplate);
            }
        }
    }

    static void OutputQueries(GeneratorExecutionContext context, ITypeSymbol type, IEnumerable<IMethodSymbol> methods, string baseApiRoute, string rootNamespace, string outputFolder, bool useRouteAsPath)
    {
        var targetFolder = GetTargetFolder(type, rootNamespace, outputFolder, useRouteAsPath, baseApiRoute);
        foreach (var queryMethod in methods.Where(_ => _.GetAttributes().Any(_ => _.IsHttpGetAttribute())))
        {
            var modelType = queryMethod.ReturnType;

            var targetFile = Path.Combine(targetFolder, $"{queryMethod.Name}.ts");
            targetFile = Path.GetFullPath(targetFile);
            var modelDescriptor = GetModelDescriptorFor(
                context,
                queryMethod,
                type,
                modelType,
                baseApiRoute,
                rootNamespace,
                outputFolder,
                useRouteAsPath,
                targetFile);
            if (modelDescriptor != ModelDescriptor.Empty)
            {
                var route = GetRoute(baseApiRoute, queryMethod);
                var importStatements = new HashSet<ImportStatement>();
                var queryArguments = GetRequestArgumentsFrom(queryMethod, ref route, importStatements);

                modelDescriptor.Imports.ForEach(_ => importStatements.Add(_));

                var queryDescriptor = new QueryDescriptor(
                    route,
                    queryMethod.Name,
                    modelDescriptor.Name,
                    modelDescriptor.Constructor,
                    modelDescriptor.IsEnumerable,
                    modelDescriptor.Imports,
                    queryArguments);

                var renderedTemplate = modelDescriptor.IsObservable ?
                    TemplateTypes.ObservableQuery(queryDescriptor) :
                    TemplateTypes.Query(queryDescriptor);
                if (renderedTemplate != default)
                {
                    WriteFile(targetFile, renderedTemplate);
                }
            }
        }
    }

    static ModelDescriptor GetModelDescriptorFor(
        GeneratorExecutionContext context,
        IMethodSymbol method,
        ITypeSymbol parentType,
        ITypeSymbol modelType,
        string baseApiRoute,
        string rootNamespace,
        string outputFolder,
        bool useRouteAsPath,
        string parentFile)
    {
        if (modelType is INamedTypeSymbol modelTypeAsNamedType)
        {
            if (modelType.ToString() == typeof(Task).FullName)
            {
                return ModelDescriptor.Empty;
            }

            if (modelTypeAsNamedType.ConstructedFrom.ToString().StartsWith(typeof(Task).FullName) && modelTypeAsNamedType.IsGenericType)
            {
                modelTypeAsNamedType = (modelTypeAsNamedType.TypeArguments[0] as INamedTypeSymbol)!;
            }
            var importStatements = new HashSet<ImportStatement>();
            var actualType = modelTypeAsNamedType;
            var isEnumerable = false;
            var observable = actualType.IsObservableClient();
            if (observable)
            {
                actualType = (actualType.TypeArguments[0] as INamedTypeSymbol)!;
            }

            if (actualType.IsEnumerable())
            {
                if (!actualType.IsGenericType)
                {
                    context.ReportDiagnostic(Diagnostics.UnableToResolveModelType($"{parentType.ToDisplayString()}:{method.Name}"));
                    return ModelDescriptor.Empty;
                }

                actualType = (actualType.TypeArguments[0] as INamedTypeSymbol)!;
                isEnumerable = true;
            }

            OutputType(context, actualType, rootNamespace, outputFolder, parentFile, importStatements, useRouteAsPath, baseApiRoute);

            var typeScriptType = actualType.GetTypeScriptType(out _);
            var knownType = actualType.IsKnownType();
            var typeName = knownType ? typeScriptType.Type : actualType.Name;
            var constructor = knownType ? typeScriptType.Constructor : actualType.Name;

            return new(typeName, constructor, isEnumerable, observable, importStatements);
        }

        return ModelDescriptor.Empty;
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
                            parameter.Type.GetTypeScriptType(out var additionalImportStatements).Type,
                            parameter.NullableAnnotation == NullableAnnotation.Annotated));

                    additionalImportStatements.ForEach(_ => importStatements.Add(_));
                }
            }
        }

        return queryArguments;
    }

    static void OutputType(
        GeneratorExecutionContext context,
        ITypeSymbol type,
        string rootNamespace,
        string outputFolder,
        string parentFile,
        HashSet<ImportStatement> parentImportStatements,
        bool useRouteAsPath,
        string baseApiRoute)
    {
        if (type.IsKnownType()) return;

        var targetFolder = GetTargetFolder(type, rootNamespace, outputFolder, useRouteAsPath, baseApiRoute);
        var targetFile = Path.Combine(targetFolder, $"{type.Name}.ts");
        var relativeImport = new Uri(parentFile).MakeRelativeUri(new Uri(targetFile));
        var relativeImportAsString = relativeImport.ToString();

        if (string.IsNullOrEmpty(relativeImportAsString))
        {
            context.ReportDiagnostic(Diagnostics.ReturnTypeWouldOverwriteParentType(type.ToDisplayString(), parentFile));
            return;
        }

        var importPath = Path.Combine(
            Path.GetDirectoryName(relativeImportAsString),
            Path.GetFileNameWithoutExtension(relativeImportAsString));

        if (Path.GetDirectoryName(targetFile) == Path.GetDirectoryName(parentFile) ||
            (!importPath.StartsWith("/") &&
            !importPath.StartsWith("../") &&
            !importPath.StartsWith("./")))
        {
            importPath = $"./{importPath}";
        }

        parentImportStatements.Add(new ImportStatement(type.Name, importPath));

        var properties = type.GetPublicPropertiesFrom();

        var typeImportStatements = new HashSet<ImportStatement>();
        var propertyDescriptors = GetPropertyDescriptorsAndOutputComplexTypes(
            context,
            rootNamespace,
            outputFolder,
            useRouteAsPath,
            baseApiRoute,
            targetFile,
            properties,
            typeImportStatements);

        string renderedTemplate = null!;

        switch (type.TypeKind)
        {
            case TypeKind.Interface:
                {
                    var typeDescriptor = new TypeDescriptor(type.Name, propertyDescriptors, typeImportStatements);
                    renderedTemplate = TemplateTypes.Interface(typeDescriptor);
                }
                break;
            case TypeKind.Class:
                {
                    var derivedType = false;
                    var derivedTypeIdentifier = string.Empty;
                    if (_derivedTypes.Any(_ => SymbolEqualityComparer.Default.Equals(_, type)))
                    {
                        var attribute = type.GetAttributes().SingleOrDefault(_ => _.AttributeClass?.ToString() == "Aksio.Cratis.Serialization.DerivedTypeAttribute");
                        if (attribute is not null)
                        {
                            derivedType = true;
                            derivedTypeIdentifier = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                        }
                    }

                    var hasPropertiesWithDerivatives = propertyDescriptors.Any(_ => _.HasDerivatives);
                    var typeDescriptor = new TypeDescriptor(type.Name, propertyDescriptors, typeImportStatements, derivedType, derivedTypeIdentifier, HasPropertiesWithDerivatives: hasPropertiesWithDerivatives);
                    renderedTemplate = TemplateTypes.Type(typeDescriptor);
                }
                break;
            case TypeKind.Enum:
                renderedTemplate = TemplateTypes.Enum(type.GetEnumDescriptor());
                break;
        }

        if (renderedTemplate != default)
        {
            Directory.CreateDirectory(targetFolder);
            WriteFile(targetFile, renderedTemplate);
        }
    }

    static IEnumerable<PropertyDescriptor> GetPropertyDescriptorsAndOutputComplexTypes(
        GeneratorExecutionContext context,
        string rootNamespace,
        string outputFolder,
        bool useRouteAsPath,
        string baseApiRoute,
        string targetFile,
        IEnumerable<IPropertySymbol> properties,
        HashSet<ImportStatement> typeImportStatements)
    {
        var propertyDescriptors = new List<PropertyDescriptor>();

        foreach (var property in properties)
        {
            var propertyType = property.Type;
            var isNullable = false;
            if (property.NullableAnnotation == NullableAnnotation.Annotated && property.Type is INamedTypeSymbol namedTypeSymbol)
            {
                isNullable = true;
                propertyType = namedTypeSymbol.TypeArguments.FirstOrDefault() ?? propertyType;
            }
            else
            {
                isNullable = property.Type.NullableAnnotation == NullableAnnotation.Annotated;
            }

            var targetType = propertyType.GetTypeScriptType(out var additionalImportStatements);
            additionalImportStatements.ForEach(_ => typeImportStatements.Add(_));
            var isEnumerable = propertyType.IsEnumerable();

            if (targetType == TypeSymbolExtensions.AnyType)
            {
                var hasDerivatives = false;
                var derivatives = new List<string>();
                var actualType = propertyType;
                var constructorType = actualType.Name;

                if (propertyType.TypeKind == TypeKind.Enum)
                {
                    constructorType = "Number";
                }
                else if (isEnumerable)
                {
                    var namedType = (INamedTypeSymbol)propertyType;
                    if (namedType.TypeArguments != default && namedType.TypeArguments.Length > 0)
                    {
                        actualType = ((INamedTypeSymbol)propertyType).TypeArguments[0];
                        constructorType = actualType.Name;
                    }
                }

                if (actualType.TypeKind == TypeKind.Interface)
                {
                    constructorType = "Object";
                    hasDerivatives = true;

                    foreach (var derivedType in _derivedTypes.ToArray().Where(_ => _.Interfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, actualType))))
                    {
                        OutputType(context, derivedType, rootNamespace, outputFolder, targetFile, typeImportStatements, useRouteAsPath, baseApiRoute);
                        derivatives.Add(derivedType.Name);
                    }
                }

                OutputType(context, actualType, rootNamespace, outputFolder, targetFile, typeImportStatements, useRouteAsPath, baseApiRoute);
                propertyDescriptors.Add(new PropertyDescriptor(property.Name, actualType.Name, constructorType, isEnumerable, isNullable, hasDerivatives, derivatives));
            }
            else
            {
                propertyDescriptors.Add(new PropertyDescriptor(property.Name, targetType.Type, targetType.Constructor, isEnumerable, isNullable, false));
            }
        }

        return propertyDescriptors;
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
            if (baseApiRoute.StartsWith(apiPrefix))
            {
                baseApiRoute = baseApiRoute.Substring(apiPrefix.Length);
            }
            if (baseApiRoute.StartsWith("/")) baseApiRoute = baseApiRoute.Substring(1);

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
