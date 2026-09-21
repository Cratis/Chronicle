// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Testing.for_TestingServices;

public class when_querying_admin_password_status_without_system_storage : given.testing_services
{
    QueryResult<AdminPasswordStatusResponse> _result;

    async Task Because() => _result = await _services.Users.GetStatus();

    [Fact] void should_not_report_success_for_unsupported_storage() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_unsupported_operation() => _result.ExceptionMessages.ShouldContainOnly(new NotSupportedException().Message);
    [Fact] void should_initialize_the_response_even_on_failure() => _result.Data.ShouldNotBeNull();
    [Fact] void should_not_fabricate_an_admin_user_id() => _result.Data.AdminUserId.ShouldBeNull();
    [Fact] void should_leave_the_admin_username_empty() => _result.Data.AdminUsername.ShouldEqual(string.Empty);
}
