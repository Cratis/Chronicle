// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>Thrown when an event is staged after a protected unit of work has started completing.</summary>
public class ProtectedUnitOfWorkEventsAfterCompletion() : Exception("Events cannot be staged after a protected unit of work has started completing.");
