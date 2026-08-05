// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// The rule and shared reasoning behind <see cref="DiagnosticIds.JoinOverridesLocalWrite"/>, which is reported
/// from both the model-bound and the fluent analyzer.
/// </summary>
static class JoinOverridesLocalWrite
{
    /// <summary>
    /// The shared descriptor for the diagnostic.
    /// </summary>
    internal static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.JoinOverridesLocalWrite,
        title: "A joined property is also written locally",
        messageFormat: "Property '{0}' is written both locally by {1} and by the join with '{2}'. The joined value is re-applied after local mappings and always takes precedence, so the local write cannot reset the property once a '{2}' event exists. If the join deliberately keeps this property fresh, suppress this diagnostic; otherwise give each fact its own property and derive the outcome from both.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A projection re-resolves its joins every time it handles one of the read model's own events and applies the joined properties after the local mappings, so a property written by both always ends up with the joined value — independent of the order the events arrived. A local write can therefore never reset or clear a joined property: the natural latch shape, where a local event sets a flag and a joined event clears it, compiles and reads correctly but silently sticks on the joined value forever. When the join is meant to keep the property fresh — seed it locally, refresh it from the joined stream — the precedence is exactly what you want; suppress the diagnostic at the site to record that intent. When the property must reflect whichever fact happened most recently, record each fact on its own property (for example two timestamps) and compare them instead of latching a single value from both sides. Collisions caused by AutoMap rather than an explicit mapping are covered separately by CHR0025.");
}
