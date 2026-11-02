// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ExplicitRegistrationEvidence;

public class when_registration_names_have_different_syntax : Specification
{
    Compilation _compilation;
    ExplicitRegistrationEvidence _evidence;

    async Task Establish()
    {
        _compilation = (await TestProject.CreateProject("""
            using System;
            namespace Cratis.Chronicle.Registrations
            {
                public class ExplicitArtifacts
                {
                    public void RegisterEventType<T>() {}
                    public void RegisterEventType(Type type) {}
                    public void RegisterReactor<T>() {}
                    public void RegisterReducer(Type type) {}
                }
            }
            public class FirstEvent {}
            public class SecondEvent {}
            public class Reactor {}
            public class Reducer {}
            public class Configuration : Cratis.Chronicle.Registrations.ExplicitArtifacts
            {
                public void Configure(Cratis.Chronicle.Registrations.ExplicitArtifacts artifacts)
                {
                    artifacts?.RegisterEventType<FirstEvent>();
                    artifacts.@RegisterEventType(typeof(SecondEvent));
                    RegisterReactor<Reactor>();
                    RegisterReducer(type: typeof(Reducer));
                }
            }
            """).GetCompilationAsync())!;
        _compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    }

    void Because() => _evidence = ExplicitRegistrationEvidence.For(_compilation, CancellationToken.None);

    [Fact] void should_recognize_a_conditional_generic_call() => _evidence.IsEventType(Type("FirstEvent"), _compilation).ShouldBeTrue();
    [Fact] void should_recognize_an_escaped_typeof_call() => _evidence.IsEventType(Type("SecondEvent"), _compilation).ShouldBeTrue();
    [Fact] void should_recognize_an_unqualified_generic_call() => _evidence.IsReactor(Type("Reactor")).ShouldBeTrue();
    [Fact] void should_recognize_an_unqualified_named_typeof_call() => _evidence.IsReducer(Type("Reducer")).ShouldBeTrue();

    INamedTypeSymbol Type(string name) => _compilation.GetTypeByMetadataName(name)!;
}
