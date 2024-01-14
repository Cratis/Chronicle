// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Schemas;

namespace Aksio.Cratis.Kernel.Projections.Expressions.ModelProperties.for_CountExpressionResolver;

public class when_trying_to_resolve_valid_count_expression_against_model_with_count_value_set : Specification
{
    CountExpressionResolver resolver;
    AppendedEvent @event;
    ExpandoObject target;

    void Establish()
    {
        target = new();
        ((dynamic)target).targetProperty = 42d;
        var content = new ExpandoObject();
        @event = new(
            new(0,
            new("02405794-91e7-4e4f-8ad1-f043070ca297", 1)),
            new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "123b8935-a1a4-410d-aace-e340d48f0aa0", "41f18595-4748-4b01-88f7-4c0d0907aa90", Enumerable.Empty<Causation>(), Identity.System),
            content);
        resolver = new(new TypeFormats());
    }

    void Because()
    {
        resolver.Resolve("targetProperty", new(), "$count()")(@event, target, ArrayIndexers.NoIndexers);
        resolver.Resolve("targetProperty", new(), "$count()")(@event, target, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_resolve_to_a_propertymapper_that_counts_into_the_property() => ((double)((dynamic)target).targetProperty).ShouldEqual(44d);
}
