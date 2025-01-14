// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Domain.Interfaces;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Events;
using AggregateRoot = Cratis.Chronicle.Orleans.Aggregates.AggregateRoot;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Domain;

public class User(IAggregateRootFactory aggregateRootFactory) : AggregateRoot, IUser
{
    public class UserDeleted(EventSourceId id) : InvalidOperationException($"User {id} is deleted");

    public StateProperty<UserName> Name = StateProperty<UserName>.Empty;
    public StateProperty<bool> Deleted = StateProperty<bool>.Empty;

    public Task Onboard(UserName name) => ApplyIfNotDeleted(new UserOnBoarded(name));
    public Task Delete() => ApplyIfNotDeleted(new Events.UserDeleted());
    public Task ChangeUserName(UserName newName) =>
        Name.Value == newName ? Task.CompletedTask : ApplyIfNotDeleted(new UserNameChanged(newName));

    public Task<bool> Exists() => Task.FromResult(!IsNew && !Deleted.Value);
#pragma warning disable CA1721
    public Task<bool> GetIsNew() => Task.FromResult(IsNew);
#pragma warning restore CA1721

    public async Task DoSomething()
    {
        var aggregate = await aggregateRootFactory.Get<IAnother>(Context!.EventSourceId);
        await Apply(new UserDidSomething());
        await aggregate.DoSomething();
    }

    Task ApplyIfNotDeleted(object evt) => Deleted.Value ? throw new UserDeleted(IdentityString) : Apply(evt);

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

    public Task<UserInternalState> GetState() => Task.FromResult(new UserInternalState(Name, Deleted));
}

public class Another : AggregateRoot, IAnother
{
    public Task DoSomething() => Apply(new SomethingHappened());
}