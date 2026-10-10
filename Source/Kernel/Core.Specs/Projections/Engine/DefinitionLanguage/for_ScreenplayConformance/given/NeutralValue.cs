// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// A value read by a mapping or a key, independent of how either lowering spells it.
/// </summary>
/// <param name="Kind">The kind of value.</param>
/// <param name="Text">The property path, context path or literal text; empty for the event source.</param>
public record NeutralValue(NeutralValueKind Kind, string Text)
{
    public static readonly NeutralValue EventSourceId = new(NeutralValueKind.EventSourceId, string.Empty);

    public override string ToString() => Kind switch
    {
        NeutralValueKind.EventSourceId => "$eventSourceId",
        NeutralValueKind.EventContext => $"$eventContext.{Text}",
        NeutralValueKind.Text => $"\"{Text}\"",
        NeutralValueKind.Unsupported => $"<unsupported {Text}>",
        _ => Text
    };
}
