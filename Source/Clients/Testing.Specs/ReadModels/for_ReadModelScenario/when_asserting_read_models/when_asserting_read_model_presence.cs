// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_asserting_read_models;

public class when_asserting_read_model_presence : a_read_model_assertion
{
    Exception _missingPartial;
    Exception _missingExact;
    Exception _present;
    Exception _absent;

    void Because()
    {
        _missingPartial = Catch.Exception(() => _scenario.ShouldPartiallyMatchReadModel("missing", new { }));
        _missingExact = Catch.Exception(() => _scenario.ShouldExactlyMatchReadModel("missing", _document));
        _present = Catch.Exception(() => _scenario.ShouldHaveNoReadModel(_key));
        _absent = Catch.Exception(() => _scenario.ShouldHaveNoReadModel("missing"));
    }

    [Fact] void should_reject_an_absent_partial_target() => _missingPartial.ShouldBeOfExactType<ReadModelAssertionFailed>();
    [Fact] void should_report_the_missing_key() => _missingPartial.Message.ShouldContain("missing");
    [Fact] void should_reject_an_absent_exact_target() => _missingExact.ShouldBeOfExactType<ReadModelAssertionFailed>();
    [Fact] void should_reject_a_present_instance() => _present.ShouldBeOfExactType<ReadModelAssertionFailed>();
    [Fact] void should_accept_an_absent_instance() => _absent.ShouldBeNull();
}
