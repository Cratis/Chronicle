// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Users;
using Cratis.Chronicle.Orleans.Aggregates;
using Events.Users;

namespace Orleans;

// public class User : AggregateRoot, IUser
// {
//     bool _isOnboarded;
//     bool _isSystem;

//     public Task OnboardSystem(UserName userName, ProfileName name) =>
//         Apply(new SystemUserAdded(userName, name, string.Empty));

//     public Task Onboard(UserName userName, ProfileName name) =>
//         Apply(new OnboardingStarted(userName, name, string.Empty));

//     public async Task ChangePassword(UserPassword password)
//     {
//         await Apply(new PasswordChanged(string.Empty));
//         if (!_isOnboarded)
//         {
//             await Apply(new OnboardingCompleted());
//         }
//     }

//     public Task RemoveUser() =>
//         Apply(new UserRemoved());

//     public Task ChangeUserName(UserName userName) =>
//         Apply(new UserNameChanged(userName));

//     public Task ChangeProfileName(ProfileName name) =>
//         Apply(new ProfileNameChanged(name));

//     public Task On(SystemUserAdded @event)
//     {
//         _isOnboarded = true;
//         _isSystem = true;
//         return Task.CompletedTask;
//     }

//     public Task On(OnboardingStarted @event)
//     {
//         _isOnboarded = false;
//         return Task.CompletedTask;
//     }

//     public Task On(OnboardingCompleted @event)
//     {
//         _isOnboarded = true;
//         return Task.CompletedTask;
//     }
// }
