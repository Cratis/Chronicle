// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ExplicitRegistrationEvidence;

public class when_the_same_compilation_is_analyzed_concurrently : Specification
{
    Compilation _compilation;
    ExplicitRegistrationEvidence[] _evidence;

    async Task Establish() => _compilation = (await TestProject.CreateProject("public class Consumer {}").GetCompilationAsync())!;

    async Task Because() => _evidence = await Task.WhenAll(Enumerable.Range(0, 32)
        .Select(_ => Task.Run(() => ExplicitRegistrationEvidence.For(_compilation, CancellationToken.None))));

    [Fact] void should_share_one_evidence_instance() => _evidence.All(evidence => ReferenceEquals(evidence, _evidence[0])).ShouldBeTrue();
    [Fact] void should_not_share_evidence_with_a_different_compilation() =>
        ReferenceEquals(_evidence[0], ExplicitRegistrationEvidence.For(_compilation.Clone(), CancellationToken.None)).ShouldBeFalse();
}
