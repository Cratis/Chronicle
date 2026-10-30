// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_keep_alive_is_not_skipped_anywhere : given.a_connection_factory
{
    void Establish() => _connection = CreateConnection(new ChronicleConnectionString("chronicle://localhost:35000"));

    [Fact] void should_not_report_keep_alive_as_skipped() => ((IConnectionKeepAlive)_connection).IsSkipped.ShouldBeFalse();
}
