// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents a read model a projection has built in memory.
/// </summary>
/// <param name="KeyValue">The key value of the read model.</param>
/// <param name="State">The state of the read model.</param>
/// <param name="LastHandledEventSequenceNumber">The sequence number of the last event that changed the read model.</param>
internal sealed record PreviewReadModel(string KeyValue, ExpandoObject State, EventSequenceNumber LastHandledEventSequenceNumber);
