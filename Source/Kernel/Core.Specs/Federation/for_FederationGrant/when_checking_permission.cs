// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationGrant;

public class when_checking_permission : Specification
{
    static readonly EventTypeId _granted = new("granted");
    static readonly EventTypeId _other = new("other");

    bool _allPublicPermitsPublic;
    bool _allPublicPermitsPrivate;
    bool _allPublicPermitsUnspecified;
    bool _specificPermitsGrantedPublic;
    bool _specificPermitsGrantedPrivate;
    bool _specificPermitsOtherPublic;

    void Because()
    {
        var allPublic = FederationGrant.AllPublicEventTypes;
        var specific = FederationGrant.For(_granted);
        _allPublicPermitsPublic = allPublic.Permits(_other, EventTypeVisibility.Public);
        _allPublicPermitsPrivate = allPublic.Permits(_other, EventTypeVisibility.Private);
        _allPublicPermitsUnspecified = allPublic.Permits(_other, EventTypeVisibility.Unspecified);
        _specificPermitsGrantedPublic = specific.Permits(_granted, EventTypeVisibility.Public);
        _specificPermitsGrantedPrivate = specific.Permits(_granted, EventTypeVisibility.Private);
        _specificPermitsOtherPublic = specific.Permits(_other, EventTypeVisibility.Public);
    }

    [Fact] void should_permit_any_public_type_for_an_all_public_grant() => _allPublicPermitsPublic.ShouldBeTrue();
    [Fact] void should_not_permit_a_private_type_for_an_all_public_grant() => _allPublicPermitsPrivate.ShouldBeFalse();
    [Fact] void should_not_permit_an_unspecified_type_for_an_all_public_grant() => _allPublicPermitsUnspecified.ShouldBeFalse();
    [Fact] void should_permit_a_granted_public_type() => _specificPermitsGrantedPublic.ShouldBeTrue();
    [Fact] void should_not_permit_a_granted_type_that_is_private() => _specificPermitsGrantedPrivate.ShouldBeFalse();
    [Fact] void should_not_permit_a_public_type_that_is_not_granted() => _specificPermitsOtherPublic.ShouldBeFalse();
}
