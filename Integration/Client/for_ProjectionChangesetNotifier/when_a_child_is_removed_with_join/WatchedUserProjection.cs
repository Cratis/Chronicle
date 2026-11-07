// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join;

/// <summary>
/// Projects users with the groups they are in; a removed group is removed from every user that holds it.
/// </summary>
public class WatchedUserProjection : IProjectionFor<WatchedUser>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<WatchedUser> builder) => builder
        .AutoMap()
        .From<UserCreated>()
        .Children(_ => _.Groups, _ => _
            .IdentifiedBy(e => e.GroupId)
            .From<UserAddedToGroup>(b => b
                .UsingParentKey(e => e.UserId))
            .RemovedWithJoin<GroupRemoved>());
}
