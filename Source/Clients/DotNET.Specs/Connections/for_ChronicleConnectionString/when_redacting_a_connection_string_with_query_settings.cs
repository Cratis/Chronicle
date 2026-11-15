// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_redacting_a_connection_string_with_query_settings : Specification
{
    string _result;

    void Because() => _result = new ChronicleConnectionString("chronicle://user:s3cret@host:35000/?skipTlsValidation=false&certificatePassword=s3cret&mySecret=s3cret").Redacted;

    [Fact] void should_not_expose_the_password() => _result.Contains("s3cret", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_username() => _result.Contains("//user", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_hide_unmodelled_query_settings() => _result.ShouldEqual("REDACTED");
}
