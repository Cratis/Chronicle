// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_asserting_read_models;

public class when_selecting_multiple_read_model_instances : a_read_model_assertion
{
    Exception _single;
    Exception _partial;
    Exception _exact;
    Exception _absent;

    void Establish() => _scenario.CollectReadModelFor("second", _document with { Name = "Second" });

    void Because()
    {
        _single = Catch.Exception(() => _ = _scenario.Instance);
        _partial = Catch.Exception(() => _scenario.ShouldPartiallyMatchReadModel("second", new { Name = "Second" }));
        _exact = Catch.Exception(() => _scenario.ShouldExactlyMatchReadModel(_key, _document));
        _absent = Catch.Exception(() => _scenario.ShouldHaveNoReadModel("missing"));
    }

    [Fact] void should_preserve_the_single_instance_guard() => _single.ShouldBeOfExactType<MultipleInstancesMaterialized>();
    [Fact] void should_allow_partial_key_selection() => _partial.ShouldBeNull();
    [Fact] void should_allow_exact_key_selection() => _exact.ShouldBeNull();
    [Fact] void should_allow_absence_with_other_models_present() => _absent.ShouldBeNull();
}
