// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_parsing_url_without_serverless : Specification
{
    ChronicleConnectionString _connectionString;

    void Because() => _connectionString = new ChronicleConnectionString("chronicle://localhost:35000");

    [Fact] void should_not_be_serverless() => _connectionString.Serverless.ShouldBeFalse();
}
