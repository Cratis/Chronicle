// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Security.for_UsersReactor;

public class when_the_user_is_missing_for_a_password_change : Specification
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => new UsersReactor(Substitute.For<IUserStorage>(), NullLogger<UsersReactor>.Instance)
        .PasswordChanged(new UserPasswordChanged("hash"), EventContext.Empty with { EventSourceId = Guid.NewGuid() }));

    [Fact] void should_fail_observation_instead_of_silently_skipping_the_password() => _exception.ShouldBeOfExactType<UserNotFound>();
}
