// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.Keys.for_CompositeKeyExpressionResolver;

public class when_resolving_a_typed_expression_with_whitespace : given.a_resolver
{
    Key _result;

    void Establish()
    {
        _eventValueProviderResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), "customerId").Returns(_ => "customer");
        _eventValueProviderResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), "orderNumber").Returns(_ => "order");
    }

    async Task Because()
    {
        var resolver = _resolver.Resolve(_projection, $"{WellKnownExpressions.Composite}( OrderKey , customerId = customerId ,  orderNumber=orderNumber )", string.Empty);
        _result = ((ResolvedKey)await resolver(null!, null!, null!)).Key;
    }

    [Fact] void should_resolve_customer_id() => ((IDictionary<string, object>)_result.Value)["customerId"].ShouldEqual("customer");
    [Fact] void should_resolve_order_number() => ((IDictionary<string, object>)_result.Value)["orderNumber"].ShouldEqual("order");
}
