// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Measures the bytes an import event carries, as the chunk limit counts them.
/// </summary>
/// <remarks>
/// The limit counts the content and the metadata that can grow without bound: tags, named tags, the causation chain and
/// the idempotency key. The .NET client measures the same way, so the chunks it cuts are never refused for their size.
/// </remarks>
internal static class ImportEventSize
{
    /// <summary>
    /// Measures an event.
    /// </summary>
    /// <param name="event">The <see cref="ImportEvent"/>.</param>
    /// <returns>The bytes it carries, as UTF-8.</returns>
    internal static long Of(ImportEvent @event)
    {
        long bytes = Utf8(@event.Content) + Utf8(@event.IdempotencyKey);
        foreach (var tag in @event.Tags ?? []) bytes += Utf8(tag);
        foreach (var tag in @event.NamedTags ?? []) bytes += Utf8(tag.Name) + Utf8(tag.Value);
        foreach (var causation in @event.Causation ?? [])
        {
            bytes += Utf8(causation.Type);
            foreach (var (name, value) in causation.Properties) bytes += Utf8(name) + Utf8(value);
        }

        return bytes;
    }

    static int Utf8(string? value) => value is null ? 0 : Encoding.UTF8.GetByteCount(value);
}
