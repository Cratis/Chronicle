// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_the_server_is_incompatible : given.a_connection_with_several_endpoints
{
    Exception? _result;

    protected override bool SkipCompatibilityCheck => false;
    protected override bool SkipKeepAlive => true;

    async Task Because()
    {
        _compatibility[_a] = () => Task.FromResult(new CompatibilityResponse { IsCompatible = false });
        try
        {
            await StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        }
        catch (Exception ex)
        {
            _result = ex;
        }
    }

    [Fact] void should_surface_the_incompatibility() => _result.ShouldBeOfExactType<IncompatibleServerException>();
    [Fact] void should_not_be_connected() => _isConnected.ShouldBeFalse();
}
