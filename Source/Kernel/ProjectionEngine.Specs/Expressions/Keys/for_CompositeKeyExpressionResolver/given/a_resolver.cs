// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.ProjectionEngine.Expressions.EventValues;
using Microsoft.Extensions.Logging.Abstractions;
using NJsonSchema;

namespace Cratis.Chronicle.ProjectionEngine.Expressions.Keys.for_CompositeKeyExpressionResolver.given;

public class a_resolver : Specification
{
    protected CompositeKeyExpressionResolver _resolver;
    protected IEventValueProviderExpressionResolvers _eventValueProviderResolvers;
    protected IProjection _projection;
    protected Model _model;

    void Establish()
    {
        _model = new("Something", new JsonSchema());
        _projection = Substitute.For<IProjection>();
        _projection.Identifier.Returns((ProjectionId)Guid.NewGuid().ToString());
        _projection.Model.Returns(_model);
        _eventValueProviderResolvers = Substitute.For<IEventValueProviderExpressionResolvers>();
        _resolver = new CompositeKeyExpressionResolver(_eventValueProviderResolvers, new KeyResolvers(NullLogger<KeyResolvers>.Instance));
    }
}
