// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

/// <summary>
/// Simulates a lost response after a real durable write.
/// </summary>
public class AcknowledgmentLost() : Exception("The committed write lost its acknowledgment.");
