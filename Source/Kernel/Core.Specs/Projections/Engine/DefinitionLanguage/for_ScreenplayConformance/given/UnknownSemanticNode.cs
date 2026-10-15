// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// The exception that is thrown when the semantic model holds a node the conformance normalizer does not know, which means
/// the pinned Screenplay version added semantics this suite has to learn before it can compare them.
/// </summary>
/// <param name="node">The unknown node.</param>
public class UnknownSemanticNode(object node) : Exception($"The semantic model node '{node}' ({node.GetType().Name}) is unknown to the conformance normalizer");
