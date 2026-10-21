// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Registers the plaintext JSON payload contract for an application or lifecycle trigger.
/// </summary>
/// <param name="Id">The stable trigger identity.</param>
/// <param name="Name">The display name.</param>
/// <param name="Schema">The JSON payload schema, without compliance metadata.</param>
public record TriggerRegistration(string Id, string Name, string Schema);
