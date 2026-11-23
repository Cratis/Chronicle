// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Namespaces.for_NamespaceBindings;

public class when_the_kernel_does_not_implement_lookup : given.a_namespace_lookup
{
    void Establish() => _failure = new RpcException(new Status(StatusCode.Unimplemented, "Unsupported"));
    Task Because() => Lookup();

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<NamespaceBindingsNotSupported>();
    [Fact] void should_not_return_a_binding() => _binding.ShouldBeNull();
    [Fact] void should_not_fall_back_to_ensure() => _namespaces.ReceivedCalls().Count().ShouldEqual(1);
}
