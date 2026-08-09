// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_redacting_a_connection_string_with_credentials : Specification
{
    const string Secret = "1b1beb83-0c3a-4c23-a6a2-bffd045356c9";

    ChronicleConnectionString _connectionString;
    string _result;

    void Establish() => _connectionString = new ChronicleConnectionString($"chronicle+srv://cratis-studio:{Secret}@chronicle.studio-production.svc.cluster.local:35000");

    void Because() => _result = _connectionString.Redacted;

    [Fact] void should_mask_the_password() => _result.ShouldEqual("chronicle+srv://cratis-studio:***@chronicle.studio-production.svc.cluster.local:35000");
    [Fact] void should_not_expose_the_password() => _result.Contains(Secret, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_leave_the_connection_string_itself_untouched() => _connectionString.ToString().ShouldEqual($"chronicle+srv://cratis-studio:{Secret}@chronicle.studio-production.svc.cluster.local:35000");
}
