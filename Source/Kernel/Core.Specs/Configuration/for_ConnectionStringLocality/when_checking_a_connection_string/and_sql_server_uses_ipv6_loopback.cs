// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionStringLocality.when_checking_a_connection_string;

public class and_sql_server_uses_ipv6_loopback : Specification
{
    [Fact] void should_identify_locality() => ConnectionStringLocality.IsNonLocal("Server=tcp:[::1],1433;Database=chronicle").ShouldBeFalse();
}
