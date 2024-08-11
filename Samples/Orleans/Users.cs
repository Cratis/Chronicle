// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Orleans.Aggregates;
using Microsoft.AspNetCore.Mvc;

namespace Orleans;

[Route("/api/users")]
public class Users(IAggregateRootFactory aggregateRootFactory, IEventLog eventLog) : ControllerBase
{
    [HttpGet("onboard")]
    public async Task FullOnboarding()
    {
        var userId = Guid.NewGuid();
        // Guid.Parse("3444635c-8174-47b3-99dd-a27cd3ea80e4");

        var user = await aggregateRootFactory.Get<IUser>(userId);
        await user.Onboard("My User", "asdasd");
        await user.ChangePassword("awesome");

        await Task.CompletedTask;
    }

    [HttpGet("onboard-events")]
    public async Task FullOnboardingEvents()
    {
        var userId = Guid.Parse("3444635c-8174-47b3-99dd-a27cd3ea80e4");
        var groupId = Guid.NewGuid();

        var result = await eventLog.Append(userId, new Events.Users.OnboardingStarted("My User", "asdasd", "asdasdasd"));
        result = await eventLog.Append(userId, new Events.Users.PasswordChanged("awesome"));
        result = await eventLog.Append(groupId, new Events.Groups.GroupAdded("My Group"));
        result = await eventLog.Append(groupId, new Events.Groups.UserAddedToGroup(userId));
        result = await eventLog.Append(userId, new Events.Users.OnboardingCompleted());
    }

    [HttpGet("rename")]
    public async Task Rename()
    {
        var userId = Guid.Parse("3444635c-8174-47b3-99dd-a27cd3ea80e4");
        var groupId = Guid.NewGuid();

        var result = await eventLog.Append(userId, new Events.Users.UserNameChanged("My User"));
    }

    [HttpGet("new")]
    public async Task New()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var result = await eventLog.Append(userId, new Events.Users.UserNameChanged("My User"));
    }
}
