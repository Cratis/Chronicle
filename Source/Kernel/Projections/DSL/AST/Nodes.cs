// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Cratis.Chronicle.Projections.DSL.AST;

/// <summary>
/// Base class for all AST nodes.
/// </summary>
public abstract record AstNode;

/// <summary>
/// Represents the root of a projection DSL document.
/// </summary>
/// <param name="Projections">Collection of projections defined in the document.</param>
public record Document(IReadOnlyList<ProjectionNode> Projections) : AstNode;

/// <summary>
/// Represents a projection definition.
/// </summary>
/// <param name="Name">The projection name.</param>
/// <param name="ReadModelType">The target read model type.</param>
/// <param name="Directives">Collection of projection-level directives and blocks.</param>
public record ProjectionNode(
    string Name,
    TypeRef ReadModelType,
    IReadOnlyList<ProjectionDirective> Directives) : AstNode;

/// <summary>
/// Base class for projection-level directives and blocks.
/// </summary>
public abstract record ProjectionDirective : AstNode;

/// <summary>
/// Represents an automap directive at projection level.
/// </summary>
public record AutoMapDirective() : ProjectionDirective;

/// <summary>
/// Represents a key declaration at projection level.
/// </summary>
/// <param name="Expression">The key expression.</param>
public record KeyDirective(Expression Expression) : ProjectionDirective;

/// <summary>
/// Represents a composite key declaration.
/// </summary>
/// <param name="TypeName">The composite key type name.</param>
/// <param name="Parts">Collection of key parts.</param>
public record CompositeKeyDirective(TypeRef TypeName, IReadOnlyList<KeyPart> Parts) : ProjectionDirective;

/// <summary>
/// Represents a part of a composite key.
/// </summary>
/// <param name="PropertyName">The property name in the composite key.</param>
/// <param name="Expression">The value expression.</param>
public record KeyPart(string PropertyName, Expression Expression) : AstNode;

/// <summary>
/// Represents a "fromEvery" block.
/// </summary>
/// <param name="Mappings">Collection of mapping operations.</param>
/// <param name="ExcludeChildren">Whether to exclude child projections.</param>
public record EveryBlock(IReadOnlyList<MappingOperation> Mappings, bool ExcludeChildren) : ProjectionDirective;

/// <summary>
/// Represents an event rule block (on EventType).
/// </summary>
/// <param name="EventType">The event type this rule applies to.</param>
/// <param name="AutoMap">Whether to automap.</param>
/// <param name="Key">Optional key specification.</param>
/// <param name="CompositeKey">Optional composite key specification.</param>
/// <param name="ParentKey">Optional parent key (for child projections).</param>
/// <param name="Mappings">Collection of mapping operations.</param>
public record OnEventBlock(
    TypeRef EventType,
    bool AutoMap,
    Expression? Key,
    CompositeKeyDirective? CompositeKey,
    Expression? ParentKey,
    IReadOnlyList<MappingOperation> Mappings) : ProjectionDirective;

/// <summary>
/// Represents a join block.
/// </summary>
/// <param name="JoinName">The name of the join.</param>
/// <param name="OnProperty">The property to join on.</param>
/// <param name="EventTypes">The event types that populate the join.</param>
/// <param name="AutoMap">Whether to automap.</param>
/// <param name="Mappings">Collection of mapping operations.</param>
public record JoinBlock(
    string JoinName,
    string OnProperty,
    IReadOnlyList<TypeRef> EventTypes,
    bool AutoMap,
    IReadOnlyList<MappingOperation> Mappings) : ProjectionDirective;

/// <summary>
/// Represents a children block.
/// </summary>
/// <param name="CollectionName">The name of the children collection property.</param>
/// <param name="IdentifierExpression">The expression that identifies children.</param>
/// <param name="AutoMap">Whether to automap at children level.</param>
/// <param name="ChildBlocks">Collection of child event handlers and joins.</param>
public record ChildrenBlock(
    string CollectionName,
    Expression IdentifierExpression,
    bool AutoMap,
    IReadOnlyList<ChildBlock> ChildBlocks) : ProjectionDirective;

/// <summary>
/// Base class for blocks that can appear within a children block.
/// </summary>
public abstract record ChildBlock : AstNode;

/// <summary>
/// Represents an event rule within a children block.
/// </summary>
/// <param name="EventType">The event type.</param>
/// <param name="Key">Optional key specification.</param>
/// <param name="CompositeKey">Optional composite key specification.</param>
/// <param name="ParentKey">Optional parent key.</param>
/// <param name="Mappings">Collection of mapping operations.</param>
public record ChildOnEventBlock(
    TypeRef EventType,
    Expression? Key,
    CompositeKeyDirective? CompositeKey,
    Expression? ParentKey,
    IReadOnlyList<MappingOperation> Mappings) : ChildBlock;

/// <summary>
/// Represents a join within a children block.
/// </summary>
/// <param name="JoinName">The name of the join.</param>
/// <param name="OnProperty">The property to join on.</param>
/// <param name="EventTypes">The event types that populate the join.</param>
/// <param name="AutoMap">Whether to automap.</param>
/// <param name="Mappings">Collection of mapping operations.</param>
public record ChildJoinBlock(
    string JoinName,
    string OnProperty,
    IReadOnlyList<TypeRef> EventTypes,
    bool AutoMap,
    IReadOnlyList<MappingOperation> Mappings) : ChildBlock;

/// <summary>
/// Represents a nested children block.
/// </summary>
/// <param name="CollectionName">The name of the children collection property.</param>
/// <param name="IdentifierExpression">The expression that identifies children.</param>
/// <param name="AutoMap">Whether to automap.</param>
/// <param name="ChildBlocks">Collection of nested child blocks.</param>
public record NestedChildrenBlock(
    string CollectionName,
    Expression IdentifierExpression,
    bool AutoMap,
    IReadOnlyList<ChildBlock> ChildBlocks) : ChildBlock;

/// <summary>
/// Represents a remove rule (removedWith).
/// </summary>
/// <param name="EventType">The event type that triggers removal.</param>
/// <param name="Key">Optional key specification.</param>
/// <param name="ParentKey">Optional parent key.</param>
public record RemoveBlock(TypeRef EventType, Expression? Key, Expression? ParentKey) : ChildBlock;

/// <summary>
/// Represents a remove via join rule.
/// </summary>
/// <param name="EventType">The event type that triggers removal.</param>
/// <param name="Key">Optional key specification.</param>
public record RemoveViaJoinBlock(TypeRef EventType, Expression? Key) : ChildBlock;

/// <summary>
/// Base class for all mapping operations.
/// </summary>
public abstract record MappingOperation : AstNode;

/// <summary>
/// Represents a property assignment.
/// </summary>
/// <param name="PropertyName">The target property name.</param>
/// <param name="Value">The value expression.</param>
public record AssignmentOperation(string PropertyName, Expression Value) : MappingOperation;

/// <summary>
/// Represents an increment operation.
/// </summary>
/// <param name="PropertyName">The property to increment.</param>
public record IncrementOperation(string PropertyName) : MappingOperation;

/// <summary>
/// Represents a decrement operation.
/// </summary>
/// <param name="PropertyName">The property to decrement.</param>
public record DecrementOperation(string PropertyName) : MappingOperation;

/// <summary>
/// Represents a count operation.
/// </summary>
/// <param name="PropertyName">The property to count into.</param>
public record CountOperation(string PropertyName) : MappingOperation;

/// <summary>
/// Represents an add operation.
/// </summary>
/// <param name="PropertyName">The property to add to.</param>
/// <param name="Value">The value to add.</param>
public record AddOperation(string PropertyName, Expression Value) : MappingOperation;

/// <summary>
/// Represents a subtract operation.
/// </summary>
/// <param name="PropertyName">The property to subtract from.</param>
/// <param name="Value">The value to subtract.</param>
public record SubtractOperation(string PropertyName, Expression Value) : MappingOperation;

/// <summary>
/// Base class for all expressions.
/// </summary>
public abstract record Expression : AstNode;

/// <summary>
/// Represents a reference to event data (e.property).
/// </summary>
/// <param name="Path">The property path within the event.</param>
public record EventDataExpression(string Path) : Expression;

/// <summary>
/// Represents a reference to event context (ctx.property).
/// </summary>
/// <param name="Property">The context property name.</param>
public record EventContextExpression(string Property) : Expression;

/// <summary>
/// Represents the $eventSourceId shorthand.
/// </summary>
public record EventSourceIdExpression() : Expression;

/// <summary>
/// Represents a literal value.
/// </summary>
/// <param name="Value">The literal value (string, number, bool, or null).</param>
public record LiteralExpression(object? Value) : Expression;

/// <summary>
/// Represents a string template with interpolated expressions.
/// </summary>
/// <param name="Parts">The template parts (strings and expressions).</param>
public record TemplateExpression(IReadOnlyList<TemplatePart> Parts) : Expression;

/// <summary>
/// Represents a part of a string template.
/// </summary>
public abstract record TemplatePart : AstNode;

/// <summary>
/// Represents a literal string part in a template.
/// </summary>
/// <param name="Text">The literal text.</param>
public record TemplateTextPart(string Text) : TemplatePart;

/// <summary>
/// Represents an interpolated expression in a template.
/// </summary>
/// <param name="Expression">The expression to interpolate.</param>
public record TemplateExpressionPart(Expression Expression) : TemplatePart;

/// <summary>
/// Represents a type reference (e.g., EventType, ReadModelType).
/// </summary>
/// <param name="Name">The type name (can be qualified with dots).</param>
public record TypeRef(string Name) : AstNode;
