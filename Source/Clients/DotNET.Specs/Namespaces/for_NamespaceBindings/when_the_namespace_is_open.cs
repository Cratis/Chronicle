// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_NamespaceBindings;

public class when_the_namespace_is_open : given.a_namespace_lookup
{
    Task Because() => Lookup();

    [Fact] void should_use_the_requested_store() => _request.EventStore.ShouldEqual("store");
    [Fact] void should_use_the_requested_name() => _request.Namespace.ShouldEqual("tenant");
    [Fact] void should_capture_the_canonical_name() => _binding.Namespace.Value.ShouldEqual("Tenant");
    [Fact] void should_capture_the_identity() => _binding.Incarnation!.Value.ShouldEqual(_response.Incarnation!.Value);
    [Fact] void should_capture_the_store() => _binding.EventStore.Value.ShouldEqual("store");
    [Fact] void should_capture_the_status() => _binding.Status.ShouldEqual(NamespaceBindingStatus.Open);
    [Fact] void should_capture_the_protocol() => _binding.ProtocolVersion.ShouldEqual(1);
    [Fact] void should_not_ensure_or_retry() => _namespaces.ReceivedCalls().Single().GetMethodInfo().Name.ShouldEqual("LookupNamespaceBinding");
    [Fact] void should_succeed() => _error.ShouldBeNull();
}
