// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnectionString;

public class when_redacting_a_connection_string_with_percent_encoded_user_information : Specification
{
    string _result;

    void Because() => _result = new ChronicleConnectionString("chronicle://us%3Aer:s3%2Fcret@host:35000").Redacted;

    [Fact] void should_not_expose_the_password() => _result.Contains("s3", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_encoded_separator() => _result.Contains("%2F", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_encoded_username() => _result.Contains("us%3Aer", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_decoded_username() => _result.Contains("us:er", StringComparison.Ordinal).ShouldBeFalse();
}
