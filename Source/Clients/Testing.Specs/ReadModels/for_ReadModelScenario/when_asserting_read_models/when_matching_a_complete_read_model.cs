// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_asserting_read_models;

public class when_matching_a_complete_read_model : a_read_model_assertion
{
    Exception _singleError;
    Exception _keyedError;

    void Because()
    {
        var expected = new
        {
            _document.Name,
            _document.Amount,
            Values = new List<int> { 1, 2 },
            Metadata = new Dictionary<string, object?> { ["note"] = null, ["owner"] = new JoinCustomerId(_customer.Value) },
            Next = (object?)null
        };
        _singleError = Catch.Exception(() => _scenario.ShouldExactlyMatchReadModel(_key, _document));
        _keyedError = Catch.Exception(() => _scenario.ShouldExactlyMatchReadModel(_key, expected));
    }

    [Fact] void should_ignore_object_runtime_type_but_match_the_complete_shape() => _singleError.ShouldBeNull();
    [Fact] void should_match_the_complete_keyed_instance() => _keyedError.ShouldBeNull();
}
