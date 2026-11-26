// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_asserting_read_models;

public class when_read_model_values_differ : a_read_model_assertion
{
    Exception _scalar;
    Exception _type;
    Exception _order;
    Exception _length;
    Exception _unknown;
    Exception _dictionary;

    void Because()
    {
        _scalar = Catch.Exception(() => _scenario.ShouldPartiallyMatchReadModel(_key, new { Name = "Grace" }));
        _type = Catch.Exception(() => _scenario.ShouldPartiallyMatchReadModel(_key, new { Amount = 12 }));
        _order = Catch.Exception(() => _scenario.ShouldPartiallyMatchReadModel(_key, new { Values = new[] { 2, 1 } }));
        _length = Catch.Exception(() => _scenario.ShouldPartiallyMatchReadModel(_key, new { Values = new[] { 1 } }));
        _unknown = Catch.Exception(() => _scenario.ShouldPartiallyMatchReadModel(_key, new { Unknown = "value" }));
        _dictionary = Catch.Exception(() => _scenario.ShouldPartiallyMatchReadModel(_key, new { Metadata = new Dictionary<string, object?> { ["owner"] = _customer } }));
    }

    [Fact] void should_use_the_domain_assertion_failure() => _scalar.ShouldBeOfExactType<ReadModelAssertionFailed>();
    [Fact] void should_identify_the_model_and_key()
    {
        _scalar.Message.ShouldContain(nameof(AssertionDocument));
        _scalar.Message.ShouldContain(_key.Value);
    }
    [Fact] void should_identify_the_property_and_values()
    {
        _scalar.Message.ShouldContain("$.Name");
        _scalar.Message.ShouldContain("Grace");
        _scalar.Message.ShouldContain("Ada");
    }
    [Fact] void should_keep_scalar_types_strict() => _type.Message.ShouldContain("$.Amount.Type");
    [Fact] void should_compare_elements_in_order() => _order.Message.ShouldContain("$.Values[0]");
    [Fact] void should_reject_a_different_collection_length() => _length.ShouldBeOfExactType<ReadModelAssertionFailed>();
    [Fact] void should_reject_unknown_properties() => _unknown.Message.ShouldContain("$.Unknown");
    [Fact] void should_compare_the_whole_dictionary_key_set() => _dictionary.Message.ShouldContain("$.Metadata.Count");
}
