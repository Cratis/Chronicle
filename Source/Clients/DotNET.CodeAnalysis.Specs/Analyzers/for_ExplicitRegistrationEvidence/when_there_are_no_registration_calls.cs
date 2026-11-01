// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ExplicitRegistrationEvidence;

public class when_there_are_no_registration_calls : Specification
{
    Compilation _compilation;
    int _semanticModelRequests;

    async Task Establish()
    {
        var calls = string.Concat(Enumerable.Repeat("System.Console.WriteLine(1);", 3000));
        _compilation = (await TestProject.CreateProject($$"""
            namespace Cratis.Chronicle.Registrations { public class ExplicitArtifacts {} }
            public class Consumer { public void Run() { {{calls}} } }
            """).GetCompilationAsync())!;
    }

    void Because() => ExplicitRegistrationEvidence.Create(_compilation, GetSemanticModel, CancellationToken.None);

    [Fact] void should_not_request_a_semantic_model_for_unrelated_invocations() => _semanticModelRequests.ShouldEqual(0);

    SemanticModel GetSemanticModel(SyntaxTree tree)
    {
        _semanticModelRequests++;
        return _compilation.GetSemanticModel(tree);
    }
}
