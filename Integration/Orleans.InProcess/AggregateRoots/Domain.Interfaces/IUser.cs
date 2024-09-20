// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Domain.Interfaces;

public record UserInternalState(StateProperty<UserName> Name, StateProperty<bool> Deleted);

public interface IUser : IIntegrationTestAggregateRoot<UserInternalState>
{
    Task Onboard(UserName name);
    Task Delete();
    Task ChangeUserName(UserName newName);
    Task<bool> Exists();
}
