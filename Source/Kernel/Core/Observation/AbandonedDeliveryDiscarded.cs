// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Prevents an abandoned reducer result from reaching the sink.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Internal delivery control flow, not a public exception contract.")]
internal sealed class AbandonedDeliveryDiscarded() : Exception("Abandoned delivery result discarded");
