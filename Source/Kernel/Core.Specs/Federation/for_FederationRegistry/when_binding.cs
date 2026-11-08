// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_binding : given.a_federation_registry
{
    IReadOnlyList<FederationBindingState> _bindings;

    async Task Because()
    {
        await _registry.Bind(Binding() with { Origin = FederationOrigin.Configuration });
        _bindings = await _registry.GetBindings();
    }

    [Fact] void should_append_one_event() => _appended.Count.ShouldEqual(1);
    [Fact] void should_append_it_on_the_binding_id() => _appended[0].Id.ShouldEqual("orders");
    [Fact] void should_append_a_peer_bound_event() => _appended[0].Content.ShouldBeOfExactType<FederationPeerBound>();
    [Fact] void should_record_runtime_origin_whatever_the_caller_said() => ((FederationPeerBound)_appended[0].Content).Origin.ShouldEqual(FederationOrigin.Runtime);
    [Fact] void should_only_refer_to_the_client_certificate_by_name() => ((FederationPeerBound)_appended[0].Content).ClientCertificate.ShouldEqual("client-cert");
    [Fact] void should_expose_the_binding() => _bindings.Single().Id.ShouldEqual("orders");
}
