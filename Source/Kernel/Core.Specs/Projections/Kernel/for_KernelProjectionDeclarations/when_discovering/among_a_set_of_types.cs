// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionDeclarations.when_discovering;

public class among_a_set_of_types : Specification
{
    IEnumerable<Type> _result;

    void Because() => _result = KernelProjectionDeclarations.Discover(
    [
        typeof(given.a_namespaced_declaration),
        typeof(given.a_dual_scoped_declaration),
        typeof(given.not_a_declaration_at_all),
        typeof(IKernelProjectionFor<given.a_namespaced_read_model>)
    ]).ToArray();

    [Fact] void should_find_the_namespaced_declaration() => _result.ShouldContain(typeof(given.a_namespaced_declaration));
    [Fact] void should_find_the_dual_scoped_declaration() => _result.ShouldContain(typeof(given.a_dual_scoped_declaration));
    [Fact] void should_not_find_an_unrelated_type() => _result.ShouldNotContain(typeof(given.not_a_declaration_at_all));
    [Fact] void should_not_find_the_interface_itself() => _result.ShouldNotContain(typeof(IKernelProjectionFor<given.a_namespaced_read_model>));
}
