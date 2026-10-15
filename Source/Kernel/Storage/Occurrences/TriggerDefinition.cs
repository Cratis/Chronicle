// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// Describes the plaintext payload contract registered for a trigger in an event store.
/// </summary>
/// <param name="Id">The stable trigger identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="Schema">The JSON schema without compliance metadata.</param>
/// <param name="Revision">The stored schema revision.</param>
public record TriggerDefinition(TriggerId Id, string Name, string Schema, long Revision);
