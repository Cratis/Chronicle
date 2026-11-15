// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_SinksOptions.when_validating;

public class and_the_read_model_default_is_unknown : Specification
{
    SinksOptions _options;
    Exception _error;

    void Establish() => _options = new()
    {
        ReadModels = "reporting"
    };

    void Because() => _error = Catch.Exception(_options.Validate);

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<InvalidSinkConfiguration>();
    [Fact] void should_name_the_configuration() => _error.Message.ShouldContain("'reporting'");
    [Fact] void should_name_the_setting() => _error.Message.ShouldContain("sinks.readModels");
}
