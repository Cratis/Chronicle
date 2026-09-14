// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Testing.for_TestingServices;

public class when_setting_initial_admin_password_with_mismatched_confirmation : given.testing_services
{
    CommandResult _result;

    async Task Because() => _result = await _services.Users.SetInitialAdminPassword(new SetInitialAdminPasswordRequest
    {
        UserId = Guid.NewGuid(),
        Password = "a-test-password",
        ConfirmedPassword = "a-different-test-password"
    });

    [Fact] void should_reject_the_password_change() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_the_kernel_password_confirmation_check() => _result.ValidationResults.Where(_ => _.Members.Contains("confirmedPassword")).Select(_ => _.Message).ShouldContainOnly("Confirmed password must match the password.");
    [Fact] void should_identify_the_invalid_member() => _result.ValidationResults.Single(_ => _.Message == "Confirmed password must match the password.").Members.ShouldContainOnly("confirmedPassword");
    [Fact] void should_not_report_an_exception() => _result.HasExceptions.ShouldBeFalse();
}
