// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Parser for the indentation-based projection DSL that converts tokens into an AST.
/// </summary>
/// <param name="tokens">The tokens to parse.</param>
public class Parser(IEnumerable<Token> tokens)
{
    readonly List<Token> _tokens = tokens.Where(t => t.Type != TokenType.NewLine).ToList();
    readonly ParsingErrors _errors = new([]);
    int _position;

    Token Current => _position < _tokens.Count ? _tokens[_position] : new Token(TokenType.EndOfInput, string.Empty, 0, 0);
    bool IsAtEnd => Current.Type == TokenType.EndOfInput;

    /// <summary>
    /// Parses the DSL into a Document AST.
    /// </summary>
    /// <returns>The parsed document or parsing errors.</returns>
    public Result<Document, ParsingErrors> Parse()
    {
        var projections = new List<ProjectionNode>();

        while (!IsAtEnd)
        {
            var projection = ParseProjection();
            if (projection is not null)
            {
                projections.Add(projection);
            }

            // If we encountered errors and couldn't parse, try to recover by advancing
            if (_errors.HasErrors && !IsAtEnd && projection is null)
            {
                Advance();
            }
        }

        return _errors.HasErrors
            ? _errors
            : new Document(projections);
    }

    Token Peek(int offset = 1) => _position + offset < _tokens.Count ? _tokens[_position + offset] : new Token(TokenType.EndOfInput, string.Empty, 0, 0);

    void Advance() => _position++;

    bool Check(TokenType type) => Current.Type == type;

    Token? Expect(TokenType type, string message = "")
    {
        if (!Check(type))
        {
            var msg = string.IsNullOrEmpty(message) ? $"Expected {type}" : message;
            _errors.Add(new SyntaxError(msg, Current.Line, Current.Column));
            return null;
        }
        var token = Current;
        Advance();
        return token;
    }

    ProjectionNode? ParseProjection()
    {
        if (!Check(TokenType.Projection))
        {
            _errors.Add(new SyntaxError("Expected 'projection'", Current.Line, Current.Column));
            return null;
        }
        Advance();

        var projectionName = ParseTypeRef();
        if (projectionName is null) return null;

        if (Expect(TokenType.Arrow) is null) return null;

        var readModelType = ParseTypeRef();
        if (readModelType is null) return null;

        if (Expect(TokenType.Indent) is null) return null;

        var directives = new List<ProjectionDirective>();
        while (!Check(TokenType.Dedent) && !IsAtEnd)
        {
            var directive = ParseProjectionDirective();
            if (directive is not null)
            {
                directives.Add(directive);
            }
        }

        if (Check(TokenType.Dedent))
        {
            Advance();
        }

        return new ProjectionNode(projectionName.Name, readModelType, directives);
    }

    ProjectionDirective? ParseProjectionDirective()
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

        if (Check(TokenType.From))
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

        _errors.Add(new SyntaxError($"Unexpected token '{Current.Value}' in projection body", Current.Line, Current.Column));
        return null;
    }

    ProjectionDirective? ParseKeyOrCompositeKeyDirective()
    {
        Advance(); // Skip 'key'

        if (Peek().Type == TokenType.LeftBrace)
        {
            var typeName = ParseTypeRef();
            if (typeName is null) return null;

            if (Expect(TokenType.LeftBrace) is null) return null;
            if (Expect(TokenType.Indent) is null) return null;

            var parts = new List<KeyPart>();
            while (!Check(TokenType.Dedent) && !IsAtEnd)
            {
                var propNameToken = Expect(TokenType.Identifier);
                if (propNameToken is null) continue;

                if (Expect(TokenType.Equals) is null) continue;

                var expr = ParseExpression();
                if (expr is not null)
                {
                    parts.Add(new KeyPart(propNameToken.Value, expr));
                }
            }

            Expect(TokenType.Dedent);
            Expect(TokenType.RightBrace);

            return new CompositeKeyDirective(typeName, parts);
        }

        var keyExpr = ParseExpression();
        return keyExpr is not null ? new KeyDirective(keyExpr) : null;
    }

    EveryBlock? ParseEveryBlock()
    {
        Advance(); // Skip 'every'
        if (Expect(TokenType.Indent) is null) return null;

        var mappings = new List<MappingOperation>();
        var excludeChildren = false;
        var autoMap = false;

        while (!Check(TokenType.Dedent) && !IsAtEnd)
        {
            if (Check(TokenType.Exclude))
            {
                Advance();
                Expect(TokenType.Children);
                excludeChildren = true;
            }
            else if (Check(TokenType.AutoMap))
            {
                Advance();
                autoMap = true;
            }
            else
            {
                var mapping = ParseMappingOperation();
                if (mapping is not null)
                {
                    mappings.Add(mapping);
                }
                else
                {
                    // Advance to prevent infinite loop on parsing errors
                    Advance();
                }
            }
        }

        Expect(TokenType.Dedent);
        return new EveryBlock(mappings, excludeChildren, autoMap);
    }

    FromEventBlock? ParseOnEventBlock()
    {
        Advance(); // Skip 'from'
        var eventType = ParseTypeRef();
        if (eventType is null) return null;

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

        if (Expect(TokenType.Indent) is null) return null;

        var mappings = new List<MappingOperation>();
        Expression? parentKey = null;
        CompositeKeyDirective? compositeKey = null;

        while (!Check(TokenType.Dedent) && !IsAtEnd)
        {
            if (Check(TokenType.AutoMap))
            {
                Advance();
                autoMap = true;
            }
            else if (Check(TokenType.Parent))
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
                var mapping = ParseMappingOperation();
                if (mapping is not null)
                {
                    mappings.Add(mapping);
                }
                else
                {
                    // Advance to prevent infinite loop on parsing errors
                    Advance();
                }
            }
        }

        Expect(TokenType.Dedent);
        return new FromEventBlock(eventType, autoMap, key, compositeKey, parentKey, mappings);
    }

    JoinBlock? ParseJoinBlock()
    {
        Advance(); // Skip 'join'
        var joinNameToken = Expect(TokenType.Identifier);
        if (joinNameToken is null) return null;
        var joinName = joinNameToken.Value;

        if (Expect(TokenType.On) is null) return null;

        var onPropertyToken = Expect(TokenType.Identifier);
        if (onPropertyToken is null) return null;
        var onProperty = onPropertyToken.Value;

        if (Expect(TokenType.Indent) is null) return null;
        if (Expect(TokenType.Events) is null) return null;

        var firstEventType = ParseTypeRef();
        if (firstEventType is null) return null;

        var eventTypes = new List<TypeRef> { firstEventType };
        while (Check(TokenType.Comma))
        {
            Advance();
            var eventType = ParseTypeRef();
            if (eventType is not null)
            {
                eventTypes.Add(eventType);
            }
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
                var mapping = ParseMappingOperation();
                if (mapping is not null)
                {
                    mappings.Add(mapping);
                }
                else
                {
                    // Advance to prevent infinite loop on parsing errors
                    Advance();
                }
            }
        }

        Expect(TokenType.Dedent);
        return new JoinBlock(joinName, onProperty, eventTypes, autoMap, mappings);
    }

    ChildrenBlock? ParseChildrenBlock()
    {
        Advance(); // Skip 'children'
        var collectionNameToken = Expect(TokenType.Identifier);
        if (collectionNameToken is null) return null;
        var collectionName = collectionNameToken.Value;

        if (Expect(TokenType.Id) is null) return null;

        var identifierExpr = ParseExpression();
        if (identifierExpr is null) return null;

        if (Expect(TokenType.Indent) is null) return null;

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
                var childBlock = ParseChildBlock();
                if (childBlock is not null)
                {
                    childBlocks.Add(childBlock);
                }
            }
        }

        Expect(TokenType.Dedent);
        return new ChildrenBlock(collectionName, identifierExpr, autoMap, childBlocks);
    }

    ChildBlock? ParseChildBlock()
    {
        if (Check(TokenType.From))
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

        _errors.Add(new SyntaxError($"Unexpected token '{Current.Value}' in children block", Current.Line, Current.Column));
        return null;
    }

    ChildOnEventBlock? ParseChildOnEventBlock()
    {
        Advance(); // Skip 'on'
        var eventType = ParseTypeRef();
        if (eventType is null) return null;

        Expression? key = null;

        // Check for inline key
        if (Check(TokenType.Key))
        {
            Advance();
            key = ParseExpression();
        }

        if (Expect(TokenType.Indent) is null) return null;

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
            else if (Check(TokenType.AutoMap))
            {
                // AutoMap is not yet supported in child event blocks, skip it
                Advance();
            }
            else
            {
                var mapping = ParseMappingOperation();
                if (mapping is not null)
                {
                    mappings.Add(mapping);
                }
                else
                {
                    // Advance to prevent infinite loop on parsing errors
                    Advance();
                }
            }
        }

        Expect(TokenType.Dedent);
        return new ChildOnEventBlock(eventType, key, compositeKey, parentKey, mappings);
    }

    ChildJoinBlock? ParseChildJoinBlock()
    {
        Advance(); // Skip 'join'
        var joinNameToken = Expect(TokenType.Identifier);
        if (joinNameToken is null) return null;
        var joinName = joinNameToken.Value;

        if (Expect(TokenType.On) is null) return null;

        var onPropertyToken = Expect(TokenType.Identifier);
        if (onPropertyToken is null) return null;
        var onProperty = onPropertyToken.Value;

        if (Expect(TokenType.Indent) is null) return null;
        if (Expect(TokenType.Events) is null) return null;

        var firstEventType = ParseTypeRef();
        if (firstEventType is null) return null;

        var eventTypes = new List<TypeRef> { firstEventType };
        while (Check(TokenType.Comma))
        {
            Advance();
            var eventType = ParseTypeRef();
            if (eventType is not null)
            {
                eventTypes.Add(eventType);
            }
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
                var mapping = ParseMappingOperation();
                if (mapping is not null)
                {
                    mappings.Add(mapping);
                }
                else
                {
                    // Advance to prevent infinite loop on parsing errors
                    Advance();
                }
            }
        }

        Expect(TokenType.Dedent);
        return new ChildJoinBlock(joinName, onProperty, eventTypes, autoMap, mappings);
    }

    NestedChildrenBlock? ParseNestedChildrenBlock()
    {
        Advance(); // Skip 'children'
        var collectionNameToken = Expect(TokenType.Identifier);
        if (collectionNameToken is null) return null;
        var collectionName = collectionNameToken.Value;

        if (Expect(TokenType.Id) is null) return null;

        var identifierExpr = ParseExpression();
        if (identifierExpr is null) return null;

        if (Expect(TokenType.Indent) is null) return null;

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
                var childBlock = ParseChildBlock();
                if (childBlock is not null)
                {
                    childBlocks.Add(childBlock);
                }
            }
        }

        Expect(TokenType.Dedent);
        return new NestedChildrenBlock(collectionName, identifierExpr, autoMap, childBlocks);
    }

    ChildBlock? ParseRemoveBlock()
    {
        Advance(); // Skip 'remove'

        // Check for "via join"
        if (Check(TokenType.Via))
        {
            Advance();
            if (Expect(TokenType.Join) is null) return null;
            if (Expect(TokenType.On) is null) return null;

            var eventType = ParseTypeRef();
            if (eventType is null) return null;

            Expression? key = null;
            if (Check(TokenType.Key))
            {
                Advance();
                key = ParseExpression();
            }

            return new RemoveViaJoinBlock(eventType, key);
        }

        if (Expect(TokenType.With) is null) return null;

        var removeEventType = ParseTypeRef();
        if (removeEventType is null) return null;

        Expression? removeKey = null;
        if (Check(TokenType.Key))
        {
            Advance();
            removeKey = ParseExpression();
        }

        if (Expect(TokenType.Indent) is null) return null;

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
                _errors.Add(new SyntaxError($"Unexpected token '{Current.Value}' in remove block", Current.Line, Current.Column));
                Advance(); // Skip invalid token to continue parsing
            }
        }

        Expect(TokenType.Dedent);
        return new RemoveBlock(removeEventType, removeKey, parentKey);
    }

    MappingOperation? ParseMappingOperation()
    {
        if (Check(TokenType.Increment))
        {
            Advance();
            var propToken = Expect(TokenType.Identifier);
            if (propToken is null) return null;
            return new IncrementOperation(propToken.Value);
        }

        if (Check(TokenType.Decrement))
        {
            Advance();
            var propToken = Expect(TokenType.Identifier);
            if (propToken is null) return null;
            return new DecrementOperation(propToken.Value);
        }

        if (Check(TokenType.Count))
        {
            Advance();
            var propToken = Expect(TokenType.Identifier);
            if (propToken is null) return null;
            return new CountOperation(propToken.Value);
        }

        if (Check(TokenType.Add))
        {
            Advance();
            var propToken = Expect(TokenType.Identifier);
            if (propToken is null) return null;

            if (Expect(TokenType.By) is null) return null;

            var value = ParseExpression();
            if (value is null) return null;
            return new AddOperation(propToken.Value, value);
        }

        if (Check(TokenType.Subtract))
        {
            Advance();
            var propToken = Expect(TokenType.Identifier);
            if (propToken is null) return null;

            if (Expect(TokenType.By) is null) return null;

            var value = ParseExpression();
            if (value is null) return null;
            return new SubtractOperation(propToken.Value, value);
        }

        // Assignment
        if (Check(TokenType.Identifier))
        {
            var propNameToken = Expect(TokenType.Identifier);
            if (propNameToken is null) return null;

            if (Expect(TokenType.Equals) is null) return null;

            var value = ParseExpression();
            if (value is null) return null;
            return new AssignmentOperation(propNameToken.Value, value);
        }

        _errors.Add(new SyntaxError("Expected mapping operation", Current.Line, Current.Column));
        return null;
    }

    Expression? ParseExpression()
    {
        // Template literal
        if (Check(TokenType.TemplateLiteral))
        {
            var template = Current.Value;
            Advance();
            return ParseTemplate(template);
        }

        // $eventSourceId or $eventContext.property
        if (Check(TokenType.Dollar))
        {
            Advance();
            var nameToken = Expect(TokenType.Identifier);
            if (nameToken is null) return null;
            var name = nameToken.Value;
            
            if (name.Equals("eventSourceId", StringComparison.OrdinalIgnoreCase))
            {
                return new EventSourceIdExpression();
            }
            
            if (name.Equals("eventContext", StringComparison.OrdinalIgnoreCase))
            {
                if (Expect(TokenType.Dot) is null) return null;
                var propertyToken = Expect(TokenType.Identifier);
                if (propertyToken is null) return null;
                return new EventContextExpression(propertyToken.Value);
            }
            
            _errors.Add(new SyntaxError($"Unknown expression '${name}'", Current.Line, Current.Column));
            return null;
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

        // Plain identifier or property path (event data)
        if (Check(TokenType.Identifier))
        {
            var path = ParsePropertyPath();
            return path is not null ? new EventDataExpression(path) : null;
        }

        _errors.Add(new SyntaxError("Expected expression", Current.Line, Current.Column));
        return null;
    }

    TemplateExpression? ParseTemplate(string template)
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
                _errors.Add(new SyntaxError("Unterminated template expression", Current.Line, Current.Column));
                return null;
            }

            // Parse the expression inside ${}
            var exprText = template.Substring(dollarIndex + 2, closeIndex - dollarIndex - 2);
            var expr = ParseTemplateExpression(exprText);
            if (expr is null) return null;
            parts.Add(new TemplateExpressionPart(expr));

            i = closeIndex + 1;
        }

        return new TemplateExpression(parts);
    }

    Expression? ParseTemplateExpression(string exprText)
    {
        // Simple parser for expressions within template
        exprText = exprText.Trim();

        if (exprText.StartsWith("$eventContext."))
        {
            return new EventContextExpression(exprText.Substring(14));
        }

        if (exprText == "$eventSourceId")
        {
            return new EventSourceIdExpression();
        }

        // Treat plain identifiers as event data property paths
        return new EventDataExpression(exprText);
    }

    string? ParsePropertyPath()
    {
        var identifierToken = Expect(TokenType.Identifier);
        if (identifierToken is null) return null;

        var parts = new List<string> { identifierToken.Value };

        while (Check(TokenType.Dot))
        {
            Advance();
            var nextToken = Expect(TokenType.Identifier);
            if (nextToken is null) return null;
            parts.Add(nextToken.Value);
        }

        return string.Join('.', parts);
    }

    TypeRef? ParseTypeRef()
    {
        var identifierToken = Expect(TokenType.Identifier);
        if (identifierToken is null) return null;

        var parts = new List<string> { identifierToken.Value };

        while (Check(TokenType.Dot))
        {
            Advance();
            var nextToken = Expect(TokenType.Identifier);
            if (nextToken is null) return null;
            parts.Add(nextToken.Value);
        }

        return new TypeRef(string.Join('.', parts));
    }
}
