// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_setting_an_invalid_grant : given.a_federation_registry
{
    Exception _specificWithoutEventTypes;
    Exception _noNamespaces;
    Exception _emptyNamespace;
    Exception _malformedPin;
    Exception _noId;

    async Task Because()
    {
        _specificWithoutEventTypes = await Catch.Exception(() => _registry.SetGrant(Grant(allPublic: false)));
        _noNamespaces = await Catch.Exception(() => _registry.SetGrant(Grant(namespaces: [])));
        _emptyNamespace = await Catch.Exception(() => _registry.SetGrant(Grant(namespaces: [" "])));
        _malformedPin = await Catch.Exception(() => _registry.SetGrant(Grant() with { ClientPin = "nope" }));
        _noId = await Catch.Exception(() => _registry.SetGrant(Grant(string.Empty)));
    }

    [Fact] void should_refuse_a_specific_grant_without_event_types() => _specificWithoutEventTypes.ShouldBeOfExactType<InvalidFederationGrant>();
    [Fact] void should_refuse_no_namespaces() => _noNamespaces.ShouldBeOfExactType<InvalidFederationGrant>();
    [Fact] void should_refuse_an_empty_namespace() => _emptyNamespace.ShouldBeOfExactType<InvalidFederationGrant>();
    [Fact] void should_refuse_a_malformed_pin() => _malformedPin.ShouldBeOfExactType<InvalidFederationGrant>();
    [Fact] void should_refuse_a_missing_id() => _noId.ShouldBeOfExactType<InvalidFederationGrant>();
    [Fact] void should_not_append_anything() => _appended.ShouldBeEmpty();
}
