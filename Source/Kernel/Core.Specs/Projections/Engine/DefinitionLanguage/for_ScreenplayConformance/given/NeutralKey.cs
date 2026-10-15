// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// A key: a single value, or a composite of named parts.
/// </summary>
/// <param name="CompositeType">The composite key type, or null for a value key.</param>
/// <param name="Parts">The parts; a value key has one part with an empty property.</param>
public record NeutralKey(string? CompositeType, IReadOnlyList<(string Property, NeutralValue Value)> Parts)
{
    public static readonly NeutralKey EventSourceId = Of(NeutralValue.EventSourceId);

    public static NeutralKey Of(NeutralValue value) => new(null, [(string.Empty, value)]);

    public override string ToString() => CompositeType is null
        ? Parts[0].Value.ToString()
        : $"{CompositeType}({string.Join(", ", Parts.OrderBy(_ => _.Property, StringComparer.Ordinal).Select(_ => $"{_.Property}={_.Value}"))})";
}
