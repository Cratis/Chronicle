// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// The exception that is thrown when an index update cannot resolve a previously validated closing scope.
/// </summary>
/// <param name="name">The owning constraint.</param>
public class InvalidClosingStreamScope(ConstraintName name) : Exception($"Cannot resolve the scope for closing constraint '{name}'.");
