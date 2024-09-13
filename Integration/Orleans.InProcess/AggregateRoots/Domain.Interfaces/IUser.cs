using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;
using IAggregateRoot = Cratis.Chronicle.Orleans.Aggregates.IAggregateRoot;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Domain.Interfaces;

public record UserInternalState(StateProperty<UserName> Name, StateProperty<bool> Deleted);

public interface IUser : IIntegrationTestAggregateRoot<UserInternalState>
{
    Task Create();
    Task Onboard(UserName name);
    Task Delete();
    Task ChangeUserName(UserName newName);
    Task<bool> Exists();
}
