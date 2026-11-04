// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService;

public class when_the_configured_password_is_shorter_than_the_minimum : given.a_new_administrator
{
    void Establish() => _authentication = new() { AdminUser = new() { Password = "tiny" } };
    Task Because() => Bootstrap();
    [Fact] void should_set_the_password() => string.IsNullOrEmpty(_administrator.PasswordHash?.Value).ShouldBeFalse();
    [Fact] void should_warn_without_logging_the_password() => _logger.Messages.ShouldContain(entry => entry.Level == LogLevel.Warning && entry.Message.Contains("recommended minimum"));
    [Fact] void should_not_log_the_password() => _logger.Messages.ShouldNotContain(entry => entry.Message.Contains("tiny", StringComparison.Ordinal));
}
