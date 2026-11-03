// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A named read model seed does not match its projection's composite key contract.
/// </summary>
/// <param name="readModelType">The read model being seeded.</param>
/// <param name="reason">The invalid part or projection contract.</param>
public class InvalidReadModelSeedKey(Type readModelType, string reason)
    : Exception($"Cannot seed read model '{readModelType.FullName}': {reason}");
