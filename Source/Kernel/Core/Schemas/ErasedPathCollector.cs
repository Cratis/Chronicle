// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Releases protected values while collecting the paths that were confirmed erased. Only the erased-property
/// reporting walk creates one, so ordinary release and apply never pay for it.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> values are released for.</param>
/// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> values are released for.</param>
internal sealed class ErasedPathCollector(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace)
{
    readonly HashSet<string> _paths = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the paths confirmed erased so far.
    /// </summary>
    public IReadOnlyCollection<string> Paths => _paths;

    /// <summary>
    /// Releases a value through a handler, recording whether its path is erased.
    /// </summary>
    /// <param name="handler">The <see cref="IJsonSchemaMetadataValueHandler"/> to release with.</param>
    /// <param name="subject">The subject identifier.</param>
    /// <param name="value">The protected value.</param>
    /// <param name="path">The path of the value.</param>
    /// <returns>The released value.</returns>
    public async Task<JsonNode> Release(IJsonSchemaMetadataValueHandler handler, string subject, JsonNode value, string path)
    {
        _paths.Remove(path);
        var reported = await handler.ReleaseReportingErasure(eventStore, eventStoreNamespace, subject, value);
        if (reported.IsErased)
        {
            _paths.Add(path);
        }

        return reported.Value;
    }
}
