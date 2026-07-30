// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureValidator.when_validating;

public class with_unsupported_constructs : given.a_capture_validator
{
    IEnumerable<CaptureValidationMessage> _result;

    async Task Because() => _result = await _validator.Validate(
        _eventStore,
        CreateDefinition(
            appends:
            [
                new AppendDefinition(EventTypeName, new WhenClause(WhenClauseType.Expression, [], Expression: "item.age > 40"), new Dictionary<string, string>()),
                new AppendDefinition(EventTypeName, new WhenClause(WhenClauseType.Added, []), new Dictionary<string, string> { ["tenant"] = "$context.tenantId" })
            ],
            map: new MapDefinition([]),
            nested: [new NestedDefinition("address", null, [])],
            children: [new ChildrenDefinition("orders", "orderId", null, [])]));

    [Fact] void should_reject_map_operations() => _result.Any(message => message.Message.Contains("Map operations")).ShouldBeTrue();
    [Fact] void should_reject_nested_scopes() => _result.Any(message => message.Message.Contains("Nested scopes")).ShouldBeTrue();
    [Fact] void should_reject_children_scopes() => _result.Any(message => message.Message.Contains("Children scopes")).ShouldBeTrue();
    [Fact] void should_reject_expression_when_clauses() => _result.Any(message => message.Message.Contains("Expression based when clauses")).ShouldBeTrue();
    [Fact] void should_reject_unsupported_assignment_expressions() => _result.Any(message => message.Message.Contains("$context.tenantId")).ShouldBeTrue();
}
