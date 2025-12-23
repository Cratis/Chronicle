// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DSL;

/// <summary>
/// Parser for the projection DSL that converts tokens into a ProjectionDefinition.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionDslParser"/> class.
/// </remarks>
/// <param name="tokens">The tokens to parse.</param>
public class ProjectionDslParser(IEnumerable<Token> tokens)
{
    readonly List<Token> _tokens = tokens.ToList();
    int _current;

    /// <summary>
    /// Parses the tokens into a ProjectionDefinition.
    /// </summary>
    /// <param name="identifier">The projection identifier.</param>
    /// <param name="owner">The projection owner.</param>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <returns>A ProjectionDefinition.</returns>
    public ProjectionDefinition Parse(
        ProjectionId identifier,
        ProjectionOwner owner,
        EventSequenceId eventSequenceId)
    {
        var readModelName = ParseReadModelName();

        var from = new Dictionary<EventType, FromDefinition>();
        var join = new Dictionary<EventType, JoinDefinition>();
        var children = new Dictionary<PropertyPath, ChildrenDefinition>();
        var removedWith = new Dictionary<EventType, RemovedWithDefinition>();
        var removedWithJoin = new Dictionary<EventType, RemovedWithJoinDefinition>();
        var fromEvery = new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false);

        while (!IsAtEnd() && CurrentToken.Type != TokenType.EndOfInput)
        {
            if (CurrentToken.Type == TokenType.Pipe)
            {
                Advance();
                ParseStatement(from, join, children, removedWith, removedWithJoin, ref fromEvery);
            }
            else
            {
                Advance();
            }
        }

        return new ProjectionDefinition(
            owner,
            eventSequenceId,
            identifier,
            new ReadModelIdentifier(readModelName),
            IsActive: true,
            IsRewindable: false,
            new JsonObject(),
            from,
            join,
            children,
            [],
            fromEvery,
            SinkDefinition.None,
            removedWith,
            removedWithJoin,
            FromEventProperty: null,
            LastUpdated: DateTimeOffset.UtcNow);
    }

    string ParseReadModelName()
    {
        if (CurrentToken.Type != TokenType.Identifier)
        {
            throw new ProjectionDslSyntaxError("Expected read model name", CurrentToken.Line, CurrentToken.Column);
        }

        var name = CurrentToken.Value;
        Advance();
        return name;
    }

    void ParseStatement(
        Dictionary<EventType, FromDefinition> from,
        Dictionary<EventType, JoinDefinition> join,
        Dictionary<PropertyPath, ChildrenDefinition> children,
        Dictionary<EventType, RemovedWithDefinition> removedWith,
        Dictionary<EventType, RemovedWithJoinDefinition> removedWithJoin,
        ref FromEveryDefinition fromEvery)
    {
        if (CurrentToken.Type == TokenType.Key)
        {
            ParseKeyStatement(from);
        }
        else if (CurrentToken.Type == TokenType.RemovedWith)
        {
            ParseRemovedWithStatement(removedWith);
        }
        else if (CurrentToken.Type == TokenType.Identifier)
        {
            var propertyName = CurrentToken.Value;
            Advance();

            if (CurrentToken.Type == TokenType.Equals)
            {
                Advance();
                if (CurrentToken.Type == TokenType.LeftBracket)
                {
                    ParseChildrenStatement(propertyName, children);
                }
                else
                {
                    ParsePropertyMappingStatement(propertyName, from, fromEvery);
                }
            }
            else if (CurrentToken.Type == TokenType.Plus)
            {
                Advance();
                ParseAddOperation(propertyName, from);
            }
            else if (CurrentToken.Type == TokenType.Minus)
            {
                Advance();
                ParseSubtractOperation(propertyName, from);
            }
            else if (Match(TokenType.Increment))
            {
                ParseIncrementStatement(propertyName, from);
            }
            else if (Match(TokenType.Decrement))
            {
                ParseDecrementStatement(propertyName, from);
            }
            else if (Match(TokenType.Count))
            {
                ParseCountStatement(propertyName, from);
            }
            else if (CurrentToken.Type == TokenType.LeftBracket)
            {
                ParseChildrenStatement(propertyName, children);
            }
        }
    }

    void ParseKeyStatement(Dictionary<EventType, FromDefinition> from)
    {
        Advance(); // Skip 'key'
        Expect(TokenType.Equals);

        if (Peek().Type == TokenType.Colon)
        {
            ParseCompositeKey(from);
        }
        else
        {
            ParseSimpleKey(from);
        }
    }

    void ParseSimpleKey(Dictionary<EventType, FromDefinition> from)
    {
        var eventTypeName = ExpectIdentifier();
        Expect(TokenType.Dot);
        var propertyName = ExpectIdentifier();

        var eventType = CreateEventType(eventTypeName);
        var keyExpression = new PropertyExpression(propertyName);

        if (from.TryGetValue(eventType, out var existing))
        {
            from[eventType] = existing with { Key = keyExpression };
        }
        else
        {
            from[eventType] = new FromDefinition(
                new Dictionary<PropertyPath, string>(),
                keyExpression,
                ParentKey: null);
        }
    }

    void ParseCompositeKey(Dictionary<EventType, FromDefinition> from)
    {
        var keyParts = new List<string>();

        while (true)
        {
            var modelProperty = ExpectIdentifier();
            Expect(TokenType.Colon);
            _ = ExpectIdentifier();
            Expect(TokenType.Dot);
            var eventProperty = ExpectIdentifier();

            keyParts.Add($"{modelProperty}={eventProperty}");

            if (CurrentToken.Type == TokenType.Comma)
            {
                Advance();
            }
            else
            {
                break;
            }
        }

        var compositeExpression = $"$composite({string.Join(',', keyParts)})";
        var eventType = EventType.Unknown;

        if (from.TryGetValue(eventType, out var existing))
        {
            from[eventType] = existing with { Key = new PropertyExpression(compositeExpression) };
        }
        else
        {
            from[eventType] = new FromDefinition(
                new Dictionary<PropertyPath, string>(),
                new PropertyExpression(compositeExpression),
                ParentKey: null);
        }
    }

    void ParsePropertyMappingStatement(
        string propertyName,
        Dictionary<EventType, FromDefinition> from,
        FromEveryDefinition fromEvery)
    {
        if (CurrentToken.Type == TokenType.Identifier && CurrentToken.Value.Equals("$eventContext", StringComparison.OrdinalIgnoreCase))
        {
            ParseEventContextMapping(propertyName, fromEvery);
        }
        else if (CurrentToken.Type == TokenType.StringLiteral || CurrentToken.Type == TokenType.NumberLiteral)
        {
            ParseConstantMapping(propertyName, from);
        }
        else
        {
            ParseEventPropertyMapping(propertyName, from);
        }
    }

    void ParseEventContextMapping(string propertyName, FromEveryDefinition fromEvery)
    {
        Advance(); // Skip $eventContext
        Expect(TokenType.Dot);
        var contextProperty = ExpectIdentifier();

        ((Dictionary<PropertyPath, string>)fromEvery.Properties)[new PropertyPath(propertyName)] = $"$eventContext.{contextProperty}";
    }

    void ParseConstantMapping(string propertyName, Dictionary<EventType, FromDefinition> from)
    {
        var constantValue = CurrentToken.Value;
        Advance();

        Expect(TokenType.On);
        var eventTypeName = ExpectIdentifier();
        var eventType = CreateEventType(eventTypeName);

        if (from.TryGetValue(eventType, out var existing))
        {
            existing.Properties[new PropertyPath(propertyName)] = $"\"{constantValue}\"";
        }
        else
        {
            var properties = new Dictionary<PropertyPath, string>
            {
                [new PropertyPath(propertyName)] = $"\"{constantValue}\""
            };
            from[eventType] = new FromDefinition(
                properties,
                PropertyExpression.NotSet,
                ParentKey: null);
        }
    }

    void ParseEventPropertyMapping(string propertyName, Dictionary<EventType, FromDefinition> from)
    {
        var eventTypeName = ExpectIdentifier();

        if (CurrentToken.Type == TokenType.Dot)
        {
            Advance();

            if (CurrentToken.Value.Equals("$eventContext", StringComparison.OrdinalIgnoreCase))
            {
                Advance();
                Expect(TokenType.Dot);
                var contextProperty = ExpectIdentifier();

                var expression = $"$eventContext.{contextProperty}";
                var eventType = CreateEventType(eventTypeName);

                if (from.TryGetValue(eventType, out var existing))
                {
                    existing.Properties[new PropertyPath(propertyName)] = expression;
                }
                else
                {
                    var properties = new Dictionary<PropertyPath, string>
                    {
                        [new PropertyPath(propertyName)] = expression
                    };
                    from[eventType] = new FromDefinition(
                        properties,
                        PropertyExpression.NotSet,
                        ParentKey: null);
                }
            }
            else
            {
                var eventProperty = CurrentToken.Value;
                Advance();

                var eventType = CreateEventType(eventTypeName);

                if (Match(TokenType.Join))
                {
                    ParseJoinProperty(propertyName, eventType, eventProperty, from);
                }
                else if (from.TryGetValue(eventType, out var existing))
                {
                    existing.Properties[new PropertyPath(propertyName)] = eventProperty;
                }
                else
                {
                    var properties = new Dictionary<PropertyPath, string>
                    {
                        [new PropertyPath(propertyName)] = eventProperty
                    };
                    from[eventType] = new FromDefinition(
                        properties,
                        PropertyExpression.NotSet,
                        ParentKey: null);
                }
            }
        }
        else
        {
            var eventType = CreateEventType(eventTypeName);

            if (from.TryGetValue(eventType, out var existing))
            {
                existing.Properties[new PropertyPath(propertyName)] = propertyName;
            }
            else
            {
                var properties = new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath(propertyName)] = propertyName
                };
                from[eventType] = new FromDefinition(
                    properties,
                    PropertyExpression.NotSet,
                    ParentKey: null);
            }
        }
    }

    void ParseJoinProperty(
        string propertyName,
        EventType eventType,
        string eventProperty,
        Dictionary<EventType, FromDefinition> from)
    {
        var joinOnProperty = ExpectIdentifier();

        if (!from.TryGetValue(eventType, out var existing))
        {
            var properties = new Dictionary<PropertyPath, string>
            {
                [new PropertyPath(propertyName)] = eventProperty
            };
            from[eventType] = new FromDefinition(
                properties,
                new PropertyExpression(joinOnProperty),
                ParentKey: null);
        }
        else
        {
            existing.Properties[new PropertyPath(propertyName)] = eventProperty;
        }
    }

    void ParseAddOperation(string propertyName, Dictionary<EventType, FromDefinition> from)
    {
        var eventTypeName = ExpectIdentifier();
        Expect(TokenType.Dot);
        var eventProperty = ExpectIdentifier();

        var eventType = CreateEventType(eventTypeName);
        var expression = $"$add({eventProperty})";

        if (from.TryGetValue(eventType, out var existing))
        {
            existing.Properties[new PropertyPath(propertyName)] = expression;
        }
        else
        {
            var properties = new Dictionary<PropertyPath, string>
            {
                [new PropertyPath(propertyName)] = expression
            };
            from[eventType] = new FromDefinition(
                properties,
                PropertyExpression.NotSet,
                ParentKey: null);
        }
    }

    void ParseSubtractOperation(string propertyName, Dictionary<EventType, FromDefinition> from)
    {
        var eventTypeName = ExpectIdentifier();
        Expect(TokenType.Dot);
        var eventProperty = ExpectIdentifier();

        var eventType = CreateEventType(eventTypeName);
        var expression = $"$subtract({eventProperty})";

        if (from.TryGetValue(eventType, out var existing))
        {
            existing.Properties[new PropertyPath(propertyName)] = expression;
        }
        else
        {
            var properties = new Dictionary<PropertyPath, string>
            {
                [new PropertyPath(propertyName)] = expression
            };
            from[eventType] = new FromDefinition(
                properties,
                PropertyExpression.NotSet,
                ParentKey: null);
        }
    }

    void ParseIncrementStatement(string propertyName, Dictionary<EventType, FromDefinition> from)
    {
        Expect(TokenType.By);
        var eventTypeName = ExpectIdentifier();

        var eventType = CreateEventType(eventTypeName);
        const string expression = "$increment";

        if (from.TryGetValue(eventType, out var existing))
        {
            existing.Properties[new PropertyPath(propertyName)] = expression;
        }
        else
        {
            var properties = new Dictionary<PropertyPath, string>
            {
                [new PropertyPath(propertyName)] = expression
            };
            from[eventType] = new FromDefinition(
                properties,
                PropertyExpression.NotSet,
                ParentKey: null);
        }
    }

    void ParseDecrementStatement(string propertyName, Dictionary<EventType, FromDefinition> from)
    {
        Expect(TokenType.By);
        var eventTypeName = ExpectIdentifier();

        var eventType = CreateEventType(eventTypeName);
        const string expression = "$decrement";

        if (from.TryGetValue(eventType, out var existing))
        {
            existing.Properties[new PropertyPath(propertyName)] = expression;
        }
        else
        {
            var properties = new Dictionary<PropertyPath, string>
            {
                [new PropertyPath(propertyName)] = expression
            };
            from[eventType] = new FromDefinition(
                properties,
                PropertyExpression.NotSet,
                ParentKey: null);
        }
    }

    void ParseCountStatement(string propertyName, Dictionary<EventType, FromDefinition> from)
    {
        var eventTypeName = ExpectIdentifier();

        var eventType = CreateEventType(eventTypeName);
        const string expression = "$count";

        if (from.TryGetValue(eventType, out var existing))
        {
            existing.Properties[new PropertyPath(propertyName)] = expression;
        }
        else
        {
            var properties = new Dictionary<PropertyPath, string>
            {
                [new PropertyPath(propertyName)] = expression
            };
            from[eventType] = new FromDefinition(
                properties,
                PropertyExpression.NotSet,
                ParentKey: null);
        }
    }

    void ParseRemovedWithStatement(Dictionary<EventType, RemovedWithDefinition> removedWith)
    {
        Advance(); // Skip 'removedWith'
        var eventTypeName = ExpectIdentifier();

        var eventType = CreateEventType(eventTypeName);
        removedWith[eventType] = new RemovedWithDefinition(
            PropertyExpression.NotSet,
            ParentKey: null);
    }

    void ParseChildrenStatement(string propertyName, Dictionary<PropertyPath, ChildrenDefinition> children)
    {
        Advance(); // Skip '['

        var childFrom = new Dictionary<EventType, FromDefinition>();
        var childJoin = new Dictionary<EventType, JoinDefinition>();
        var childChildren = new Dictionary<PropertyPath, ChildrenDefinition>();
        var childRemovedWith = new Dictionary<EventType, RemovedWithDefinition>();
        var childRemovedWithJoin = new Dictionary<EventType, RemovedWithJoinDefinition>();
        var childFromEvery = new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false);
        var identifiedBy = PropertyPath.NotSet;

        while (CurrentToken.Type != TokenType.RightBracket && !IsAtEnd())
        {
            if (CurrentToken.Type == TokenType.Pipe)
            {
                Advance();

                if (CurrentToken.Type == TokenType.Identified)
                {
                    Advance();
                    Expect(TokenType.By);
                    identifiedBy = new PropertyPath(ExpectIdentifier());
                }
                else
                {
                    ParseStatement(childFrom, childJoin, childChildren, childRemovedWith, childRemovedWithJoin, ref childFromEvery);
                }
            }
            else
            {
                Advance();
            }
        }

        Expect(TokenType.RightBracket);

        children[new PropertyPath(propertyName)] = new ChildrenDefinition(
            identifiedBy,
            childFrom,
            childJoin,
            childChildren,
            childFromEvery,
            childRemovedWith,
            childRemovedWithJoin,
            FromEventProperty: null);
    }

    EventType CreateEventType(string eventTypeName)
    {
        return new EventType(new EventTypeId(eventTypeName), EventTypeGeneration.First);
    }

    Token CurrentToken => _tokens[_current];

    bool IsAtEnd() => _current >= _tokens.Count;

    Token Peek()
    {
        if (_current + 1 < _tokens.Count)
        {
            return _tokens[_current + 1];
        }
        return new Token(TokenType.EndOfInput, string.Empty, 0, 0);
    }

    void Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }
    }

    bool Match(TokenType type)
    {
        if (CurrentToken.Type == type)
        {
            Advance();
            return true;
        }
        return false;
    }

    void Expect(TokenType type)
    {
        if (CurrentToken.Type != type)
        {
            throw new ProjectionDslSyntaxError(
                $"Expected {type} but found {CurrentToken.Type}",
                CurrentToken.Line,
                CurrentToken.Column);
        }
        Advance();
    }

    string ExpectIdentifier()
    {
        if (CurrentToken.Type != TokenType.Identifier)
        {
            throw new ProjectionDslSyntaxError(
                $"Expected identifier but found {CurrentToken.Type}",
                CurrentToken.Line,
                CurrentToken.Column);
        }
        var value = CurrentToken.Value;
        Advance();
        return value;
    }
}
