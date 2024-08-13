// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Orleans.Aggregates;

namespace Orleans;

public interface IUser : IAggregateRoot
{
    Task OnboardSystem(string userName, string name);
    Task Onboard(string userName, string name);
    Task ChangeUserName(string userName);
    Task ChangeProfileName(string name);
    Task ChangePassword(string password);
    Task RemoveUser();
}
