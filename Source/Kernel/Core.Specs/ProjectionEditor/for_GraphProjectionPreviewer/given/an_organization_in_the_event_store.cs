// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor.for_GraphProjectionPreviewer.given;

/// <summary>
/// An event store with two people, two groups, memberships and a nested group.
/// </summary>
public class an_organization_in_the_event_store : a_previewer
{
    void Establish() => _events =
    [
        Event(1, Ada, new PersonRegistered(Ada, "Ada")),
        Event(2, Grace, new PersonRegistered(Grace, "Grace")),
        Event(3, Engineering, new GroupCreated(Engineering, "Engineering")),
        Event(4, Platform, new GroupCreated(Platform, "Platform")),
        Event(5, Ada, new MemberAdded(Ada, Engineering)),
        Event(6, Grace, new MemberAdded(Grace, Platform)),
        Event(7, Platform, new GroupNested(Platform, Engineering))
    ];
}
