// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelStore.when_checking_if_materialized;

public class and_the_read_model_is_passive : given.a_materialized_read_model
{
    bool _result;

    void Because() => _result = _store.IsMaterialized(_definition with { Sink = SinkDefinition.None });

    [Fact] void should_not_be_materialized() => _result.ShouldBeFalse();
}
