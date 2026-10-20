// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ConnectionStringLocality.when_checking_a_connection_string;

public class and_postgresql_is_local : Specification
{
    [Fact] void should_identify_locality() => ConnectionStringLocality.IsNonLocal("Host=127.0.0.1;Database=chronicle").ShouldBeFalse();
}
