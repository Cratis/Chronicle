// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_making_an_ordinary_call_afterwards : given.a_connection_with_ambient_state
{
    string _callAmbient;
    Activity _callActivity;

    async Task Because()
    {
        await _connection.Connect();
        await _keepAliveAmbient.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _correlationIdAccessor.Current.Returns(_ =>
        {
            _callAmbient = _ambient.Value;
            _callActivity = Activity.Current;
            return CorrelationId.New();
        });
        _ambient.Value = "request-tenant";
        _activity = new Activity("request").Start();
        await ((IChronicleServicesAccessor)_connection).Services.Connections.ConnectionKeepAlive(new ConnectionKeepAlive());
    }

    [Fact] void should_pass_the_request_ambient_state_to_the_ordinary_call() => _callAmbient.ShouldEqual("request-tenant");
    [Fact] void should_pass_the_request_activity_to_the_ordinary_call() => _callActivity.ShouldBeSame(_activity);
}
