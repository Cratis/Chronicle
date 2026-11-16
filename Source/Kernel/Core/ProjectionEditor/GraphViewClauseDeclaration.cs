// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents a clause that selects the roots of a view when reading a preview. Clauses only select roots; a relation always
/// holds every related item.
/// </summary>
/// <param name="Relation">The name of a relation member of the view; the clause then keeps the roots that have a related item satisfying <paramref name="Member"/> and <paramref name="Value"/>, or any related item when there is no <paramref name="Member"/>.</param>
/// <param name="Member">The name of a property member, of the view, or of the related view when there is a <paramref name="Relation"/>.</param>
/// <param name="Value">The value the property member must equal, compared as text.</param>
public record GraphViewClauseDeclaration(string? Relation = null, string? Member = null, string? Value = null);
