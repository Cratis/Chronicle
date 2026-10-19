// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleOptions.when_setting_reactors_to_null;

public class and_reducers_to_null : Specification
{
    ChronicleOptions _options;
    Exception _reactorsError;
    Exception _reducersError;

    void Establish() => _options = new();

    void Because()
    {
        _reactorsError = Catch.Exception(() => _options.Reactors = null!);
        _reducersError = Catch.Exception(() => _options.Reducers = null!);
    }

    [Fact] void should_reject_null_reactor_options() => _reactorsError.ShouldBeOfExactType<ArgumentNullException>();
    [Fact] void should_reject_null_reducer_options() => _reducersError.ShouldBeOfExactType<ArgumentNullException>();
    [Fact] void should_keep_reactor_options() => _options.Reactors.ShouldNotBeNull();
    [Fact] void should_keep_reducer_options() => _options.Reducers.ShouldNotBeNull();
}
