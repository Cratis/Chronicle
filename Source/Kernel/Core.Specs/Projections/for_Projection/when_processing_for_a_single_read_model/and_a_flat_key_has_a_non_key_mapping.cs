// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_a_single_read_model;

public class and_a_flat_key_has_a_non_key_mapping : given.a_projection_grain_with_a_child_projection
{
    ExpandoObject _result;

    void Establish()
    {
        RootOperationType = ProjectionOperationType.From;
        _rootProjection.ChildProjections.Returns([]);
        _rootKey = new FlatCompositeKey(new { customerId = "customer", orderNumber = "one" }.AsExpandoObject(), ArrayIndexers.NoIndexers);
        _event = _event with { Content = new { age = 42 }.AsExpandoObject() };
        ProjectRootWith(context => context.Changeset.SetProperties(
            [PropertyMappers.FromEventValueProvider("amount", incoming => ((IDictionary<string, object?>)incoming.Content)["age"]!)],
            ArrayIndexers.NoIndexers));
    }

    async Task Because() => _result = await ProcessTheEvent();

    [Fact]
    void should_keep_the_seeded_identity_when_mapping_clones_the_state()
    {
        var state = (IDictionary<string, object?>)_result;
        state["customerId"].ShouldEqual("customer");
        state["orderNumber"].ShouldEqual("one");
        state["amount"].ShouldEqual(42);
        state.ContainsKey("id").ShouldBeFalse();
    }
}
