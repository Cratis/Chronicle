// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DSL.AST;

namespace Cratis.Chronicle.Projections.DSL;

/// <summary>
/// Parser for the indentation-based projection DSL that converts tokens into an AST.
/// </summary>
public class RulesProjectionDslParser
{
    readonly List<Token> _tokens;
    int _position;

    /// <summary>
    /// Initializes a new instance of the <see cref="RulesProjectionDslParser"/> class.
    /// </summary>
    /// <param name="tokens">The tokens to parse.</param>
    public RulesProjectionDslParser(IEnumerable<Token> tokens)
    {
        _tokens = tokens.Where(t => t.Type != TokenType.NewLine).ToList();
        _position = 0;
    }

    Token Current => _position < _tokens.Count ? _tokens[_position] : new Token(TokenType.EndOfInput, string.Empty, 0, 0);
    Token Peek(int offset = 1) => _position + offset < _tokens.Count ? _tokens[_position + offset] : new Token(TokenType.EndOfInput, string.Empty, 0, 0);
    bool IsAtEnd => Current.Type == TokenType.EndOfInput;

    void Advance() => _position++;

    bool Check(TokenType type) => Current.Type == type;

    bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    Token Expect(TokenType type, string message = "")
    {
        if (!Check(type))
        {
            var msg = string.IsNullOrEmpty(message) ? $"Expected {type}" : message;
            throw new ProjectionDslSyntaxError(msg, Current.Line, Current.Column);
        }
        var token = Current;
        Advance();
        return token;
    }

    /// <summary>
    /// Parses the DSL into a Document AST.
    /// </summary>
    /// <returns>The parsed document.</returns>
    public Document Parse()
    {
        var projections = new List<ProjectionNode>();

        while (!IsAtEnd)
        {
            projections.Add(ParseProjection());
        }

        return new Document(projections);
    }

    ProjectionNode ParseProjection()
    {
        Expect(TokenType.Projection);
        var name = Expect(TokenType.Identifier).Value;
        Expect(TokenType.Arrow);
        var readModelType = ParseTypeRef();

        Expect(TokenType.Indent);

        var directives = new List<ProjectionDirective>();
        while (!Check(TokenType.Dedent) && !IsAtEnd)
        {
            directives.Add(ParseProjectionDirective());
        }

        if (Check(TokenType.Dedent))
        {
            Advance();
        }

        return new ProjectionNode(name, readModelType, directives);
    }

    ProjectionDirective ParseProjectionDirective()
    {
        if (Check(TokenType.AutoMap))
        {
            Advance();
            return new AutoMapDirective();
        }

        if (Check(TokenType.Key))
        {
            return ParseKeyOrCompositeKeyDirective();
        }

        if (Check(TokenType.Every))
        {
            return ParseEveryBlock();
        }

        if (Check(TokenType.On))
        {
            return ParseOnEventBlock();
        }

        if (Check(TokenType.Join))
        {
            return ParseJoinBlock();
        }

        if (Check(TokenType.Children))
        {
            return ParseChildrenBlock();
        }

        throw new ProjectionDslSyntaxError($"Unexpected token '{Current.Value}' in projection body", Current.Line, Current.Column);
    }

    ProjectionDirective ParseKeyOrCompositeKeyDirective()
    {
        Advance(); // Skip 'key'

        if (Peek().Type == TokenType.LeftBrace)
        {
            var typeName = ParseTypeRef();
            Expect(TokenType.LeftBrace);
            Expect(TokenType.Indent);

            var parts = new List<KeyPart>();
            while (!Check(TokenType.Dedent) && !IsAtEnd)
            {
                var propName = Expect(TokenType.Identifier).Value;
                Expect(TokenType.Equals);
                var expr = ParseExpression();
                parts.Add(new KeyPart(propName, expr));
            }

            Expect(TokenType.Dedent);
            Expect(TokenType.RightBrace);

            return new CompositeKeyDirective(typeName, parts);
        }

        var keyExpr = ParseExpression();
        return new KeyDirective(keyExpr);
    }

    EveryBlock ParseEveryBlock()
    {
        Advance(); // Skip 'every'
        Expect(TokenType.Indent);

        var mappings = new List<MappingOperation>();
        var excludeChildren = false;

        while (!Check(TokenType.Dedent) && !IsAtEnd)
        {
            if (Check(TokenType.Exclude))
            {
                Advance();
                Expect(TokenType.Children);
                excludeChildren = true;
            }
            else
            {
                mappings.Add(ParseMappingOperation());
            }
        }

        Expect(TokenType.Dedent);
        return new EveryBlock(mappings, excludeChildren);
    }

    OnEventBlock ParseOnEventBlock()
    {
        Advance(); // Skip 'on'
        var eventType = ParseTypeRef();

        var autoMap = false;
        Expression? key = null;

        // Check for inline options
        while (!Check(TokenType.Indent) && !IsAtEnd)
        {
            if (Check(TokenType.AutoMap))
            {
                Advance();
                autoMap = true;
            }
            else if (Check(TokenType.Key))
            {
                Advance();
                key = ParseExpression();
            }
            else
            {
                break;
            }
        }

        Expect(TokenType.Indent);

        var mappings = new List<MappingOperation>();
        Expression? parentKey = null;
        CompositeKeyDirective? compositeKey = null;

        while (!Check(TokenType.Dedent) && !IsAtEnd)
        {
            if (Check(TokenType.Parent))
            {
                Advance();
                parentKey = ParseExpression();
            }
            else if (Check(TokenType.Key))
            {
                var keyDirective = ParseKeyOrCompositeKeyDirective();
                if (keyDirective is CompositeKeyDirective ck)
                {
                    compositeKey = ck;
                }
                else if (keyDirective is KeyDirective kd)
                {
                    key = kd.Expression;
                }
            }
            else
            {
                mappings.Add(ParseMappingOperation());
            }
        }

        Expect(TokenType.Dedent);
        return new OnEventBlock(eventType, autoMap, key, compositeKey, parentKey, mappings);
    }

    JoinBlock ParseJoinBlock()
    {
        Advance(); // Skip 'join'
        var joinName = Expect(TokenType.Identifier).Value;
        Expect(TokenType.On);
        var onProperty = Expect(TokenType.Identifier).Value;

        Expect(TokenType.Indent);
        Expect(TokenType.Events);

        var eventTypes = new List<TypeRef> { ParseTypeRef() };
        while (Check(TokenType.Comma))
        {
            Advance();
            eventTypes.Add(ParseTypeRef());
        }

        var autoMap = false;
        var mappings = new List<MappingOperation>();

        while (!Check(TokenType.Dedent) && !IsAtEnd)
        {
            if (Check(TokenType.AutoMap))
            {
                Advance();
                autoMap = true;
            }
            else
            {
                mappings.Add(ParseMappingOperation());
            }
        }

        Expect(TokenType.Dedent);
        return new JoinBlock(joinName, onProperty, eventTypes, autoMap, mappings);
    }

    ChildrenBlock ParseChildrenBlock()
    {
        Advance(); // Skip 'children'
        var collectionName = Expect(TokenType.Identifier).Value;
        Expect(TokenType.Id);
        var identifierExpr = ParseExpression();

        Expect(TokenType.Indent);

        var autoMap = false;
        var childBlocks = new List<ChildBlock>();

        while (!Check(TokenType.Dedent) && !IsAtEnd)
        {
            if (Check(TokenType.AutoMap))
            {
                Advance();
                autoMap = true;
            }
            else
            {
                childBlocks.Add(ParseChildBlock());
            }
        }

        Expect(TokenType.Dedent);
        return new ChildrenBlock(collectionName, identifierExpr, autoMap, childBlocks);
    }

    ChildBlock ParseChildBlock()
    {
        if (Check(TokenType.On))
        {
            return ParseChildOnEventBlock();
        }

        if (Check(TokenType.Join))
        {
            return ParseChildJoinBlock();
        }

        if (Check(TokenType.Children))
        {
            return ParseNestedChildrenBlock();
        }

        if (Check(TokenType.Remove))
        {
            return ParseRemoveBlock();
        }

        throw new ProjectionDslSyntaxError($"Unexpected token '{Current.Value}' in children block", Current.Line, Current.Column);
    }

    ChildOnEventBlock ParseChildOnEventBlock()
    {
        Advance(); // Skip 'on'
        var eventType = ParseTypeRef();

        Expression? key = null;

        // Check for inline key
        if (Check(TokenType.Key))
        {
            Advance();
            key = ParseExpression();
        }

        Expect(TokenType.Indent);

        var mappings = new List<MappingOperation>();
        Expression? parentKey = null;
        CompositeKeyDirective? compositeKey = null;

        while (!Check(TokenType.Dedent) && !IsAtEnd)
        {
            if (Check(TokenType.Parent))
            {
                Advance();
                parentKey = ParseExpression();
            }
            else if (Check(TokenType.Key))
            {
                var keyDirective = ParseKeyOrCompositeKeyDirective();
                if (keyDirective is CompositeKeyDirective ck)
                {
                    compositeKey = ck;
                }
                else if (keyDirective is KeyDirective kd)
                {
                    key = kd.Expression;
                }
            }
            else
            {
                mappings.Add(ParseMappingOperation());
            }
        }

        Expect(TokenType.Dedent);
        return new ChildOnEventBlock(eventType, key, compositeKey, parentKey, mappings);
    }

    ChildJoinBlock ParseChildJoinBlock()
    {
        Advance(); // Skip 'join'
        var joinName = Expect(TokenType.Identifier).Value;
        Expect(TokenType.On);
        var onProperty = Expect(TokenType.Identifier).Value;

        Expect(TokenType.Indent);
        Expect(TokenType.Events);

        var eventTypes = new List<TypeRef> { ParseTypeRef() };
        while (Check(TokenType.Comma))
        {
            Advance();
            eventTypes.Add(ParseTypeRef());
        }

        var autoMap = false;
        var mappings = new List<MappingOperation>();

        while (!Check(TokenType.Dedent) && !IsAtEnd)
        {
            if (Check(TokenType.AutoMap))
            {
                Advance();
                autoMap = true;
            }
            else
            {
                mappings.Add(ParseMappingOperation());
            }
        }

        Expect(TokenType.Dedent);
        return new ChildJoinBlock(joinName, onProperty, eventTypes, autoMap, mappings);
    }

    NestedChildrenBlock ParseNestedChildrenBlock()
    {
        Advance(); // Skip 'children'
        var collectionName = Expect(TokenType.Identifier).Value;
        Expect(TokenType.Id);
        var identifierExpr = ParseExpression();

        Expect(TokenType.Indent);

        var autoMap = false;
        var childBlocks = new List<ChildBlock>();

        while (!Check(TokenType.Dedent) && !IsAtEnd)
        {
            if (Check(TokenType.AutoMap))
            {
                Advance();
                autoMap = true;
            }
            else
            {
                childBlocks.Add(ParseChildBlock());
            }
        }

        Expect(TokenType.Dedent);
        return new NestedChildrenBlock(collectionName, identifierExpr, autoMap, childBlocks);
    }

    ChildBlock ParseRemoveBlock()
    {
        Advance(); // Skip 'remove'

        // Check for "via join"
        if (Check(TokenType.Via))
        {
            Advance();
            Expect(TokenType.Join);
            Expect(TokenType.On);
            var eventType = ParseTypeRef();

            Expression? key = null;
            if (Check(TokenType.Key))
            {
                Advance();
                key = ParseExpression();
            }

            return new RemoveViaJoinBlock(eventType, key);
        }

        Expect(TokenType.On);
        var removeEventType = ParseTypeRef();

        Expression? removeKey = null;
        if (Check(TokenType.Key))
        {
            Advance();
            removeKey = ParseExpression();
        }

        Expect(TokenType.Indent);

        Expression? parentKey = null;
        while (!Check(TokenType.Dedent) && !IsAtEnd)
        {
            if (Check(TokenType.Parent))
            {
                Advance();
                parentKey = ParseExpression();
            }
            else
            {
                throw new ProjectionDslSyntaxError($"Unexpected token '{Current.Value}' in remove block", Current.Line, Current.Column);
            }
        }

        Expect(TokenType.Dedent);
        return new RemoveBlock(removeEventType, removeKey, parentKey);
    }

    MappingOperation ParseMappingOperation()
    {
        if (Check(TokenType.Increment))
        {
            Advance();
            var prop = Expect(TokenType.Identifier).Value;
            return new IncrementOperation(prop);
        }

        if (Check(TokenType.Decrement))
        {
            Advance();
            var prop = Expect(TokenType.Identifier).Value;
            return new DecrementOperation(prop);
        }

        if (Check(TokenType.Count))
        {
            Advance();
            var prop = Expect(TokenType.Identifier).Value;
            return new CountOperation(prop);
        }

        if (Check(TokenType.Add))
        {
            Advance();
            var prop = Expect(TokenType.Identifier).Value;
            Expect(TokenType.By);
            var value = ParseExpression();
            return new AddOperation(prop, value);
        }

        if (Check(TokenType.Subtract))
        {
            Advance();
            var prop = Expect(TokenType.Identifier).Value;
            Expect(TokenType.By);
            var value = ParseExpression();
            return new SubtractOperation(prop, value);
        }

        // Assignment
        if (Check(TokenType.Identifier))
        {
            var propName = Expect(TokenType.Identifier).Value;
            Expect(TokenType.Equals);
            var value = ParseExpression();
            return new AssignmentOperation(propName, value);
        }

        throw new ProjectionDslSyntaxError($"Expected mapping operation", Current.Line, Current.Column);
    }

    Expression ParseExpression()
    {
        // Template literal
        if (Check(TokenType.TemplateLiteral))
        {
            var template = Current.Value;
            Advance();
            return ParseTemplate(template);
        }

        // Event reference (e.property)
        if (Check(TokenType.EventRef))
        {
            Advance();
            Expect(TokenType.Dot);
            var path = ParsePropertyPath();
            return new EventDataExpression(path);
        }

        // Context reference (ctx.property)
        if (Check(TokenType.ContextRef))
        {
            Advance();
            Expect(TokenType.Dot);
            var property = Expect(TokenType.Identifier).Value;
            return new EventContextExpression(property);
        }

        // $eventSourceId shorthand
        if (Check(TokenType.Dollar))
        {
            Advance();
            var name = Expect(TokenType.Identifier).Value;
            if (name.Equals("eventSourceId", StringComparison.OrdinalIgnoreCase))
            {
                return new EventSourceIdExpression();
            }
            throw new ProjectionDslSyntaxError($"Unknown shorthand '${name}'", Current.Line, Current.Column);
        }

        // Literals
        if (Check(TokenType.True))
        {
            Advance();
            return new LiteralExpression(true);
        }

        if (Check(TokenType.False))
        {
            Advance();
            return new LiteralExpression(false);
        }

        if (Check(TokenType.Null))
        {
            Advance();
            return new LiteralExpression(null);
        }

        if (Check(TokenType.NumberLiteral))
        {
            var value = Current.Value;
            Advance();
            return new LiteralExpression(double.Parse(value));
        }

        if (Check(TokenType.StringLiteral))
        {
            var value = Current.Value;
            Advance();
            return new LiteralExpression(value);
        }

        throw new ProjectionDslSyntaxError($"Expected expression", Current.Line, Current.Column);
    }

    Expression ParseTemplate(string template)
    {
        var parts = new List<TemplatePart>();
        var i = 0;

        while (i < template.Length)
        {
            var dollarIndex = template.IndexOf("${", i);
            if (dollarIndex == -1)
            {
                // No more interpolations
                if (i < template.Length)
                {
                    parts.Add(new TemplateTextPart(template.Substring(i)));
                }
                break;
            }

            // Add text before interpolation
            if (dollarIndex > i)
            {
                parts.Add(new TemplateTextPart(template.Substring(i, dollarIndex - i)));
            }

            // Find closing brace
            var closeIndex = template.IndexOf('}', dollarIndex + 2);
            if (closeIndex == -1)
            {
                throw new ProjectionDslSyntaxError("Unterminated template expression", Current.Line, Current.Column);
            }

            // Parse the expression inside ${}
            var exprText = template.Substring(dollarIndex + 2, closeIndex - dollarIndex - 2);
            var expr = ParseTemplateExpression(exprText);
            parts.Add(new TemplateExpressionPart(expr));

            i = closeIndex + 1;
        }

        return new TemplateExpression(parts);
    }

    Expression ParseTemplateExpression(string exprText)
    {
        // Simple parser for expressions within template
        exprText = exprText.Trim();

        if (exprText.StartsWith("e."))
        {
            return new EventDataExpression(exprText.Substring(2));
        }

        if (exprText.StartsWith("ctx."))
        {
            return new EventContextExpression(exprText.Substring(4));
        }

        throw new ProjectionDslSyntaxError($"Invalid template expression '{exprText}'", Current.Line, Current.Column);
    }

    string ParsePropertyPath()
    {
        var parts = new List<string> { Expect(TokenType.Identifier).Value };

        while (Check(TokenType.Dot))
        {
            Advance();
            parts.Add(Expect(TokenType.Identifier).Value);
        }

        return string.Join(".", parts);
    }

    TypeRef ParseTypeRef()
    {
        var parts = new List<string> { Expect(TokenType.Identifier).Value };

        while (Check(TokenType.Dot))
        {
            Advance();
            parts.Add(Expect(TokenType.Identifier).Value);
        }

        return new TypeRef(string.Join(".", parts));
    }
}
