// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

/// <summary>
/// Signals invalid v2 authority or opaque claim-token input before any mutation.
/// </summary>
/// <param name="field">The invalid field, never its contents.</param>
public sealed class InvalidUniqueConstraintV2Input(string field) : Exception($"Invalid unique constraint v2 input: {field}.");
