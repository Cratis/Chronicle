// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_Options.when_parsing;

public class and_self_test_is_requested : Specification
{
    static readonly string[] _arguments = ["--self-test", "--current", "/protos/chronicle.desc"];

    Options _result;

    void Because() => _result = Options.Parse(_arguments);

    [Fact] void should_not_require_a_baseline() => _result.SelfTest.ShouldBeTrue();
    [Fact] void should_keep_the_current_descriptor() => _result.Current.ShouldEqual("/protos/chronicle.desc");
}
