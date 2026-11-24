// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

/// <summary>
/// Represents an opaque 32-character lowercase hexadecimal v2 scope token, including caller-encoded unscoped tokens, not a logical scope string.
/// </summary>
/// <param name="Value">The encoded scope token.</param>
public sealed record UniqueConstraintStorageScope(string Value) : ConceptAs<string>(Validate(Value))
{
    static string Validate(string value) => value.Length == 32 && value.All(_ => _ is >= '0' and <= '9' or >= 'a' and <= 'f')
        ? value
        : throw new InvalidUniqueConstraintV2Input(nameof(UniqueConstraintStorageScope));
}
