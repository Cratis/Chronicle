// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.Keys.for_CompositeKeyExpressionResolver.given;

public class a_resolver : Specification
{
    protected CompositeKeyExpressionResolver resolver;
    protected Mock<IEventValueProviderExpressionResolvers> event_value_provider_resolvers;
    protected Mock<IProjection> projection;
    protected Model model;

    void Establish()
    {
        model = new("Something", new JsonSchema());
        projection = new();
        projection.SetupGet(_ => _.Identifier).Returns((ProjectionId)Guid.NewGuid().ToString());
        projection.SetupGet(_ => _.Model).Returns(model);
        event_value_provider_resolvers = new();
        resolver = new CompositeKeyExpressionResolver(event_value_provider_resolvers.Object);
    }
}
