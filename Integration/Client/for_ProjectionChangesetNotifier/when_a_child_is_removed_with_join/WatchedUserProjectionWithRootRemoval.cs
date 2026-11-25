// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join;

/// <summary>
/// The same projection, but a user can also be removed itself.
/// </summary>
public class WatchedUserProjectionWithRootRemoval : IProjectionFor<WatchedUser>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<WatchedUser> builder) => builder
        .AutoMap()
        .From<UserCreated>()
        .RemovedWith<UserRemoved>()
        .Children(_ => _.Groups, _ => _
            .IdentifiedBy(e => e.GroupId)
            .From<UserAddedToGroup>(b => b
                .UsingParentKey(e => e.UserId))
            .RemovedWithJoin<GroupRemoved>());
}
