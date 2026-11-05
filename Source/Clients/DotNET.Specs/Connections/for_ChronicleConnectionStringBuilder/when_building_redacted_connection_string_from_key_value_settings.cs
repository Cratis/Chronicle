// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionStringBuilder;

public class when_building_redacted_connection_string_from_key_value_settings : Specification
{
    string _result;

    void Because() => _result = new ChronicleConnectionStringBuilder("Host=host;Port=35000;Username=user;Password=s3cret;pwd=s3cret").BuildRedacted();

    [Fact] void should_not_expose_the_password() => _result.Contains("s3cret", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_username() => _result.Contains("=user", StringComparison.Ordinal).ShouldBeFalse();
}
