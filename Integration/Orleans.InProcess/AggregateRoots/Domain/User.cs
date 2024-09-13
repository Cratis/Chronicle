using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Domain.Interfaces;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Events;
using AggregateRoot = Cratis.Chronicle.Orleans.Aggregates.AggregateRoot;
using IAggregateRootFactory = Cratis.Chronicle.Orleans.Aggregates.IAggregateRootFactory;

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
    public Task<CorrelationId> GetCorrelationId() => Task.FromResult(Context!.UnitOfWOrk.CorrelationId);
    public Task<bool> GetIsNew() => Task.FromResult(IsNew);

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
