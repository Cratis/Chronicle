// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to indicate that a single nullable property represents a nested projection object.
/// The nested type should carry its own <see cref="FromEventAttribute{TEvent}"/> and optionally <see cref="ClearWithAttribute{TEvent}"/> attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NestedAttribute : Attribute, IProjectionAnnotation, INestedAttribute;
