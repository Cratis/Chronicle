// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator.given;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator;

public class when_generating_namespace_routed_queries : given.a_generated_service
{
    const string CasingStub = """
        namespace Cratis.Chronicle.Namespaces;
        public interface INamespaceCasing
        {
            System.Threading.Tasks.ValueTask<Cratis.Chronicle.Concepts.EventStoreNamespaceName> Canonicalize(
                Cratis.Chronicle.Concepts.EventStoreName eventStore,
                Cratis.Chronicle.Concepts.EventStoreNamespaceName @namespace);
        }
        """;

    static readonly int _skipSegments = typeof(CasingReadModel).Namespace!.Split('.').Length - 1;

    string _source;
    IReadOnlyList<Microsoft.CodeAnalysis.Diagnostic> _errors;

    void Establish()
    {
        _serviceDefinition = new("CasingQueries", typeof(CasingReadModel).Namespace!);
        var methods = typeof(CasingReadModel).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(method => string.Equals(method.Name, nameof(CasingReadModel.ForNamespace), StringComparison.Ordinal) ||
                string.Equals(method.Name, nameof(CasingReadModel.ForApplication), StringComparison.Ordinal) ||
                string.Equals(method.Name, nameof(CasingReadModel.ForStore), StringComparison.Ordinal) ||
                string.Equals(method.Name, nameof(CasingReadModel.ObserveNamespace), StringComparison.Ordinal))
            .Select(method => new QueryMethodDefinition(method)).ToList();
        _serviceDefinition.Queries.Add(new QueryDefinition(typeof(CasingReadModel), methods));
        _serviceDefinition.KeyedQueries.Add(new KeyedQueryDefinition(typeof(ICasingGrain), typeof(CasingKey), typeof(ICasingGrain).GetMethod(nameof(ICasingGrain.HasData))!));
    }

    void Because()
    {
        _contractCode = new ServiceInterfaceGenerator(_skipSegments, ContractsNamespace).Generate(_serviceDefinition, _outputDirectory);
        _source = File.ReadAllText(new ServiceImplementationGenerator(_skipSegments, ContractsNamespace, ImplementationsNamespace).Generate(_serviceDefinition, _outputDirectory).Path);
        _ = typeof(KeyHelper).Assembly;
        (_errors, _) = GeneratedSourceCompiler.Compile(_contractCode, _source, CasingStub);
    }

    [Fact] void should_compile() => _errors.ShouldBeEmpty();
    [Fact] void should_inject_the_namespace_casing_resolver() => _source.ShouldContain("global::Cratis.Chronicle.Namespaces.INamespaceCasing namespaceCasing");
    [Fact] void should_resolve_before_constructing_the_grain_key() => Body("HasData").IndexOf("namespaceCasing.Canonicalize", StringComparison.Ordinal).ShouldBeLessThan(Body("HasData").IndexOf("KeyHelper.Combine", StringComparison.Ordinal));
    [Fact] void should_resolve_synchronous_read_model_queries() => Body("ForNamespace").ShouldContain("request.Namespace = (await namespaceCasing.Canonicalize(request.EventStore, request.Namespace)).Value;");
    [Fact] void should_resolve_inside_the_guarded_query_delegate() => Body("ForNamespace").ShouldContain("async () =>\n            {\n                if (!string.IsNullOrEmpty(request.Namespace))");
    [Fact] void should_not_resolve_store_only_queries() => Body("ForStore").ShouldNotContain("namespaceCasing");
    [Fact] void should_skip_application_scope_queries() => Body("ForApplication").ShouldContain("!string.IsNullOrEmpty(request.Namespace) && !request.ApplicationScope");
    [Fact] void should_defer_observable_queries_until_resolution_completes() => Body("ObserveNamespace").ShouldContain("QueryExecutor.Canonicalize(async () =>");

    string Body(string name)
    {
        var start = _source.IndexOf($" {name}(", StringComparison.Ordinal);
        var end = _source.IndexOf("    ///", start, StringComparison.Ordinal);
        return end < 0 ? _source[start..] : _source[start..end];
    }
}
