// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Integration.Specifications.AggregateRoots.Concepts;
using Cratis.Chronicle.Integration.Specifications.AggregateRoots.Events;
using AggregateRoot = Cratis.Chronicle.Aggregates.AggregateRoot;
using IAggregateRootFactory = Cratis.Chronicle.Aggregates.IAggregateRootFactory;

namespace Cratis.Chronicle.Integration.Specifications.AggregateRoots.Domain;

public class User(IAggregateRootFactory aggregateRootFactory) : AggregateRoot
{
    public class UserDeleted(EventSourceId id) : InvalidOperationException($"User {id} is deleted");

    public StateProperty<UserName> Name = StateProperty<UserName>.Empty;
    public StateProperty<bool> Deleted = StateProperty<bool>.Empty;

    public Task Onboard(UserName name) => ApplyIfNotDeleted(new UserOnBoarded(name));
    public Task Delete() => ApplyIfNotDeleted(new Events.UserDeleted());
    public Task ChangeUserName(UserName newName) =>
        Name.Value == newName ? Task.CompletedTask : ApplyIfNotDeleted(new UserNameChanged(newName));

    Task ApplyIfNotDeleted(object evt) => Deleted.Value ? throw new UserDeleted(EventSourceId.Unspecified) : Apply(evt);

    public Task On(UserOnBoarded evt)
    {
        Name = Name.New(evt.Name);
        return Task.CompletedTask;
    }

    public Task On(UserDeleted evt)
    {
        Deleted = Deleted.New(true);
        return Task.CompletedTask;
    }

    public Task On(UserNameChanged evt)
    {
        Name = Name.New(evt.NewName);
        return Task.CompletedTask;
    }
}
