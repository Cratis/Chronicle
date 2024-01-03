// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Projections.Expressions.EventValues;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Projections.Expressions.Keys.for_CompositeKeyExpressionResolver.given;

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
        projection.SetupGet(_ => _.Identifier).Returns(Guid.NewGuid());
        projection.SetupGet(_ => _.Model).Returns(model);
        event_value_provider_resolvers = new();
        resolver = new CompositeKeyExpressionResolver(event_value_provider_resolvers.Object);
    }
}
