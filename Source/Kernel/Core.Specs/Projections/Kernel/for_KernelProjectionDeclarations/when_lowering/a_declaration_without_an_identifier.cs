// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Catch = Cratis.Specifications.Catch;

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionDeclarations.when_lowering;

/// <summary>
/// The identifier is what a definition is stored, retired and recognized under, so it cannot fall back to a type
/// name that is free to change. Failing loudly at startup beats a projection that quietly declines to register and
/// leaves whatever reads its read model answering nothing.
/// </summary>
public class a_declaration_without_an_identifier : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => KernelProjectionDeclarations.Lower(typeof(given.a_declaration_without_an_identifier)));

    [Fact] void should_refuse_to_lower_it() => _exception.ShouldBeOfExactType<KernelProjectionWithoutIdentifier>();
}
