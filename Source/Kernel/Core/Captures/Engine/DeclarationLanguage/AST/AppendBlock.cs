// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.AST;

/// <summary>
/// Represents an append block.
/// </summary>
/// <param name="EventType">The event type to append.</param>
/// <param name="When">The when clause.</param>
/// <param name="Assignments">The field assignments.</param>
public record AppendBlock(
    string EventType,
    WhenClauseNode When,
    IReadOnlyList<FieldAssignmentNode> Assignments) : CaptureDirective;
