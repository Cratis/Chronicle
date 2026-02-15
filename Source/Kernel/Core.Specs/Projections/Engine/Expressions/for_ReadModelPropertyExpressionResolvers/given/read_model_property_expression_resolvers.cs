// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.for_ReadModelPropertyExpressionResolvers.given;

public class read_model_property_expression_resolvers : Specification
{
    protected ReadModelPropertyExpressionResolvers _resolvers;
    protected IEventValueProviderExpressionResolvers _eventValueResolvers;

    void Establish()
    {
        _eventValueResolvers = Substitute.For<IEventValueProviderExpressionResolvers>();
        _resolvers = new ReadModelPropertyExpressionResolvers(_eventValueResolvers, new TypeFormats(), Substitute.For<ILogger<ReadModelPropertyExpressionResolvers>>());
    }
}
