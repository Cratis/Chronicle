// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Occurrences.for_OccurrencesStorage.given;

/// <summary>
/// Simulates losing the acknowledgment of a real committed write.
/// </summary>
public class AcknowledgmentLost() : Exception("The committed write lost its acknowledgment.");
