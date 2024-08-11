// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Orleans.Aggregates;
using Events.Users;

namespace Orleans;

public class User : AggregateRoot, IUser
{
    bool _isOnboarded;
    bool _isSystem;

    public Task OnboardSystem(string userName, string name) =>
        Apply(new SystemUserAdded(userName, name, string.Empty));

    public Task Onboard(string userName, string name) =>
        Apply(new OnboardingStarted(userName, name, string.Empty));

    public async Task ChangePassword(string password)
    {
        await Apply(new PasswordChanged(string.Empty));
        if (!_isOnboarded)
        {
            await Apply(new OnboardingCompleted());
        }
    }

    public Task RemoveUser() =>
        Apply(new UserRemoved());

    public Task ChangeUserName(string userName) =>
        Apply(new UserNameChanged(userName));

    public Task ChangeProfileName(string name) =>
        Apply(new ProfileNameChanged(name));

    public Task On(SystemUserAdded @event)
    {
        _isOnboarded = true;
        _isSystem = true;
        return Task.CompletedTask;
    }

    public Task On(OnboardingStarted @event)
    {
        _isOnboarded = false;
        return Task.CompletedTask;
    }

    public Task On(OnboardingCompleted @event)
    {
        _isOnboarded = true;
        return Task.CompletedTask;
    }
}
