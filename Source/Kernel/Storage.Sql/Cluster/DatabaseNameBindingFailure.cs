// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// The exception that is thrown when a database name cannot be safely bound.
/// </summary>
/// <param name="message">The binding failure.</param>
#pragma warning disable CA1064 // Kernel-only diagnostic; no public client contract is introduced.
internal sealed class DatabaseNameBindingFailure(string message) : Exception(message);
