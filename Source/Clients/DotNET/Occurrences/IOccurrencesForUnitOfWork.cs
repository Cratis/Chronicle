// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Allows only the trusted unit commit to record NoEvents after proving it has no protected scopes.
/// </summary>
internal interface IOccurrencesForUnitOfWork
{
    /// <summary>
    /// Records an outcome after the trusted commit has checked its read contributions.
    /// </summary>
    /// <param name="key">The enrolled effect.</param>
    /// <param name="response">The response snapshot.</param>
    /// <returns>The winning receipt.</returns>
    Task<EffectReceiptResult> RecordNoEvents(EffectKey key, JsonNode? response);
}
