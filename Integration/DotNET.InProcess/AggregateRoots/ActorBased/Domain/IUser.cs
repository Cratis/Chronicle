// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.AggregateRoots.Concepts;

namespace Cratis.Chronicle.InProcess.Integration.AggregateRoots.ActorBased.Domain;

public interface IUser : IIntegrationTestAggregateRoot<UserInternalState>
{
    Task Onboard(UserName name);
    Task Delete();
    Task ChangeUserName(UserName newName);
    Task<bool> Exists();
}
