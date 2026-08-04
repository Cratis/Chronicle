// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Attribute used to disable automatic property mapping for a projection model.
/// </summary>
/// <remarks>
/// Applied at class (or struct) level, it prevents AutoMap from mapping any property automatically, and is
/// inherited by child projections. Applied at property or parameter level, it excludes only that single
/// property from AutoMap while every other property keeps mapping — use it to stop an unrelated event that
/// carries an identically named property from silently overwriting a property whose value is set explicitly
/// (for example via <c>[SetFrom]</c>).
/// <para>
/// Both forms apply wherever the attribute is written: on the root read model, on a <c>[ChildrenFrom]</c> child
/// record and on a <c>[Nested]</c> type. Exclusions are scoped to the model that declares them and are matched by
/// property name, so a child or nested type declares its own and does not inherit the root's - one model's
/// exclusion cannot silently blank an unrelated property of the same name on another.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Parameter, Inherited = true)]
public sealed class NoAutoMapAttribute : Attribute;
