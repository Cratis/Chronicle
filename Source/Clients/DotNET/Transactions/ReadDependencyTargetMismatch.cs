// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a dependency belongs to another store, namespace or sequence.
/// </summary>
/// <param name="description">The dependency.</param>
public class ReadDependencyTargetMismatch(string description) : Exception($"The read dependency '{description}' does not target this unit of work's event store, namespace and sequence.");
