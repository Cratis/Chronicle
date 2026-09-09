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
    [Fact] void should_not_report_a_fabricated_admin_status() => _result.Data.ShouldBeNull();
}
