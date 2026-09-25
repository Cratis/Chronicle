// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionDeclarations.given;

[KernelProjection("a-namespaced-one")]
public class a_namespaced_declaration : IKernelProjectionFor<a_namespaced_read_model>
{
    public void Define(IKernelProjectionBuilder<a_namespaced_read_model> builder) => builder
        .FromEvery(from => from.Count(_ => _.Count));
}
