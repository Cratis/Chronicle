// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionDeclarations.given;

[KernelProjection("a-dual-scoped-one")]
public class a_dual_scoped_declaration : IKernelProjectionFor<a_dual_scoped_read_model>
{
    public void Define(IKernelProjectionBuilder<a_dual_scoped_read_model> builder) => builder
        .ScopedTo(ProjectionScope.Both)
        .IdentifiedByComposite(key => key
            .With(_ => _.EventType, "$eventContext(EventType.Id)")
            .With(_ => _.Namespace, "$eventContext(Namespace)"))
        .FromEvery(from => from.Count(_ => _.Count));
}
