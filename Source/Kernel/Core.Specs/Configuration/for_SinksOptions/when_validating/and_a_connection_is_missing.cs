// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_SinksOptions.when_validating;

public class and_a_connection_is_missing : Specification
{
    SinksOptions _options;
    Exception _error;

    void Establish() => _options = new()
    {
        Configurations = new Dictionary<string, SinkConfiguration> { ["reporting"] = new() { Type = "SQL" } }
    };

    void Because() => _error = Catch.Exception(_options.Validate);

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<InvalidSinkConfiguration>();
    [Fact] void should_name_the_configuration() => _error.Message.ShouldContain("'reporting'");
}
