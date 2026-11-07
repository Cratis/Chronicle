// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Security.for_EventStoreAccessEvents;

public class when_validating_against_the_kernel_schema : given.kernel_entitlement_schemas
{
    static readonly EventStoreAccessGranted _wildcardGrant = new(_holder, StoreAccess.Read | StoreAccess.Append, EventStoreAccessGrantSource.Configuration);
    static readonly EventStoreAccessGranted _explicitGrant = new(_holder, StoreAccess.Read, EventStoreAccessGrantSource.Command, EventStore: "shop", Namespace: "tenant", Proof: "proof");

    [Fact] void should_omit_the_wildcard_store_and_namespace() => Serialize(_wildcardGrant).ShouldNotContain(_ => _.Key == "eventStore" || _.Key == "namespace");
    [Fact] void should_accept_a_wildcard_grant() => Validate(_wildcardGrant).ShouldBeEmpty();
    [Fact] void should_accept_a_wildcard_revocation() => Validate(new EventStoreAccessRevoked(_holder)).ShouldBeEmpty();
    [Fact] void should_accept_an_explicit_grant() => Validate(_explicitGrant).ShouldBeEmpty();
    [Fact] void should_round_trip_an_explicit_grant() => Serialize(_explicitGrant).Deserialize<EventStoreAccessGranted>(_options).ShouldEqual(_explicitGrant);
    [Fact] void should_round_trip_a_wildcard_grant() => Serialize(_wildcardGrant).Deserialize<EventStoreAccessGranted>(_options).ShouldEqual(_wildcardGrant);
    [Fact] void should_round_trip_an_explicit_revocation() => Serialize(new EventStoreAccessRevoked(_holder, "shop", "tenant")).Deserialize<EventStoreAccessRevoked>(_options).ShouldEqual(new EventStoreAccessRevoked(_holder, "shop", "tenant"));

    [Theory]
    [InlineData("holder")]
    [InlineData("access")]
    [InlineData("source")]
    public void should_still_require_the_grant_identity_and_access(string property) =>
        Validate(_wildcardGrant, property).ShouldContain(error => error.Kind == JsonSchemaValidationErrorKind.PropertyRequired && error.Path == property);

    [Fact] void should_still_require_the_revoked_holder() =>
        Validate(new EventStoreAccessRevoked(_holder), "holder").ShouldContain(error => error.Kind == JsonSchemaValidationErrorKind.PropertyRequired && error.Path == "holder");
}
