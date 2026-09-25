// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Attribute used to declare that a read model member is deliberately not populated by the projection, because
/// whatever reads the model assembles it.
/// </summary>
/// <remarks>
/// A projection may only consume events, never another read model, so a value that has to be joined in from a
/// second read model can only be filled in by the query that serves the model. Without a way to say so, such a
/// member is indistinguishable from one whose mapping was simply forgotten - which is exactly what the analyzers
/// report it as. This states the intent, so the member is left alone: AutoMap will not bind it even when a
/// subscribed event happens to carry the same name, and it is not reported as unmapped.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotProjectedAttribute : Attribute, IProjectionAnnotation;
