// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// A single <c language="csharp">[ChildrenFrom]</c> declaration on a member of a projection model.
/// </summary>
/// <param name="MemberName">The member name on the projection model.</param>
/// <param name="MemberType">The type of the member.</param>
/// <param name="Attribute">The ChildrenFrom attribute being processed.</param>
/// <param name="EventType">The event type to create children from.</param>
/// <param name="ParentModelType">The type of the parent model that contains the children collection.</param>
internal sealed record ChildrenFromDeclaration(
    string MemberName,
    Type MemberType,
    Attribute Attribute,
    Type EventType,
    Type? ParentModelType);
