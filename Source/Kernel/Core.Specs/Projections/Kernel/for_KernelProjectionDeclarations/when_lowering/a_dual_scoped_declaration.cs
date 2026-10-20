// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionDeclarations.when_lowering;

public class a_dual_scoped_declaration : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => KernelProjectionDeclarations.Lower(typeof(given.a_dual_scoped_declaration)));

    [Fact] void should_refuse_dual_scope_materialization() => _error.InnerException.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_explain_the_generation_boundary() => _error.InnerException.Message.ShouldContain("0014");
}
