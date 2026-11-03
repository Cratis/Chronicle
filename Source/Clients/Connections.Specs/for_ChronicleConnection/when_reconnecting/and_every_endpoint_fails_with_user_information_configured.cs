// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_reconnecting;

public class and_every_endpoint_fails_with_user_information_configured : given.a_connection_with_several_endpoints
{
    const string Secret = "s3cret";

    Exception? _result;

    protected override ChronicleConnectionString ConnectionString => new("chronicle://user:s3cret@localhost:35000");
    protected override bool SkipCompatibilityCheck => false;

    async Task Because()
    {
        // The first endpoint fails with a transport message that carries the secret; the second is incompatible.
        _compatibility[_a] = () => Task.FromException<CompatibilityResponse>(new RpcException(new Status(StatusCode.Unavailable, "dial chronicle://user:s3cret@node-a:35000 failed")));
        _compatibility[_b] = () => Task.FromResult(new CompatibilityResponse { IsCompatible = false, ServerVersion = "1.0.0" });
        _result = await Catch.Exception(() => StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System));
    }

    [Fact] void should_have_logged_something() => _logger.Messages.ShouldNotBeEmpty();
    [Fact] void should_not_expose_the_password_in_logged_messages() => _logger.Messages.ShouldNotContain(message => message.Contains(Secret, StringComparison.Ordinal));
    [Fact] void should_not_expose_the_username_in_logged_messages() => _logger.Messages.ShouldNotContain(message => message.Contains("//user", StringComparison.Ordinal));
    [Fact] void should_not_expose_the_password_in_structured_values() => _logger.States.SelectMany(state => state).SelectMany(argument => given.diagnostic_payloads.Text(argument.Value)).ShouldNotContain(text => text.Contains(Secret, StringComparison.Ordinal));
    [Fact] void should_not_expose_the_password_in_logged_exceptions() => _logger.Exceptions.SelectMany(given.diagnostic_payloads.Text).ShouldNotContain(text => text.Contains(Secret, StringComparison.Ordinal));
    [Fact] void should_not_expose_the_password_in_the_thrown_exception() => (_result?.ToString() ?? string.Empty).Contains(Secret, StringComparison.Ordinal).ShouldBeFalse();
}
