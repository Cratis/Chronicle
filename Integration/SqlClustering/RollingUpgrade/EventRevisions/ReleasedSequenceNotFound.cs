// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.EventRevisions;

/// <summary>
/// The exception that is thrown when the released kernel did not persist the bootstrap event.
/// </summary>
public sealed class ReleasedSequenceNotFound() : Exception("The released kernel's event sequence witness was not found.");
