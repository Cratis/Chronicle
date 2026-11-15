// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_setting_a_grant : given.a_federation_registry
{
    IReadOnlyList<FederationGrantState> _grants;
    FederationGrantSet _event;

    async Task Because()
    {
        await _registry.SetGrant(Grant(allPublic: true, eventTypes: ["ignored"]));
        _event = (FederationGrantSet)_appended[0].Content;
        _grants = await _registry.GetGrants();
    }

    [Fact] void should_append_on_the_link_id() => _appended[0].Id.ShouldEqual("link-1");
    [Fact] void should_record_runtime_origin() => _event.Origin.ShouldEqual(FederationOrigin.Runtime);
    [Fact] void should_not_keep_event_types_for_an_all_public_grant() => _event.EventTypes.ShouldBeEmpty();
    [Fact] void should_expose_the_grant_as_active() => _grants.Single().Revoked.ShouldBeFalse();
}
