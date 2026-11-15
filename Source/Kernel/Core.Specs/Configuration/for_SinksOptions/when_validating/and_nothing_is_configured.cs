// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_SinksOptions.when_validating;

public class and_nothing_is_configured : Specification
{
    SinksOptions _options;
    Exception _error;

    void Establish() => _options = new();

    void Because() => _error = Catch.Exception(_options.Validate);

    [Fact] void should_accept_it() => _error.ShouldBeNull();
}
