// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Security.for_UserProjection;

public class when_the_projection_does_not_complete : Specification
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => UserProjection.WaitForPassword(Substitute.For<IUserStorage>(), UserId.NotSet, "hash", TimeSpan.Zero));

    [Fact] void should_report_failure_instead_of_password_success() => _exception.ShouldBeOfExactType<UserProjectionDidNotComplete>();
}
