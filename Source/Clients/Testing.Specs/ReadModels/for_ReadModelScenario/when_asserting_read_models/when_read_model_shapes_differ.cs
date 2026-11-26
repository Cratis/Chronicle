// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_asserting_read_models;

public class when_read_model_shapes_differ : a_read_model_assertion
{
    Exception _omitted;
    Exception _extra;
    Exception _null;
    Exception _nested;

    void Establish() => _document.Next = new("Nested", 1m, [], new Dictionary<string, JoinCustomerId?>());

    void Because()
    {
        _omitted = Catch.Exception(() => _scenario.ShouldExactlyMatchReadModel(_key, new { Name = "Ada" }));
        _extra = Catch.Exception(() => _scenario.ShouldExactlyMatchReadModel(_key, new { _document.Name, _document.Amount, _document.Values, _document.Metadata, _document.Next, Unknown = 0 }));
        _null = Catch.Exception(() => _scenario.ShouldPartiallyMatchReadModel(_key, new { Next = (object?)null }));
        _nested = Catch.Exception(() => _scenario.ShouldPartiallyMatchReadModel(_key, new { Next = new { Name = "Other" } }));
    }

    [Fact] void should_reject_omitted_exact_properties() => _omitted.ShouldBeOfExactType<ReadModelAssertionFailed>();
    [Fact] void should_reject_extra_expected_properties() => _extra.Message.ShouldContain("$.Unknown");
    [Fact] void should_treat_null_as_a_value() => _null.Message.ShouldContain("$.Next");
    [Fact] void should_report_nested_property_paths() => _nested.Message.ShouldContain("$.Next.Name");
}
