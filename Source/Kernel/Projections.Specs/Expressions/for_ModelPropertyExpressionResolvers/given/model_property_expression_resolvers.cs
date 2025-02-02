// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Expressions.for_ModelPropertyExpressionResolvers.given;

public class model_property_expression_resolvers : Specification
{
    protected ModelPropertyExpressionResolvers _resolvers;
    protected IEventValueProviderExpressionResolvers _eventValueResolvers;

    void Establish()
    {
        _eventValueResolvers = Substitute.For<IEventValueProviderExpressionResolvers>();
        _resolvers = new ModelPropertyExpressionResolvers(_eventValueResolvers, new TypeFormats());
    }
}
