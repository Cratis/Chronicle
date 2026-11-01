// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_no_application_version_is_specified : given.a_connection_factory
{
    ConnectRequest _request;

    void Because()
    {
        _connection = CreateConnection("chronicle://localhost:35000");
        _request = _connection.CreateConnectRequest();
    }

    [Fact] void should_keep_the_entry_assembly_version() => _request.ClientVersion.ShouldEqual(ClientProcess.Version);
}
