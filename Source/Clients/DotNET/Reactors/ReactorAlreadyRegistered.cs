// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// The exception that is thrown when a reactor identifier is already registered in this client.
/// </summary>
/// <param name="id">The duplicate reactor identifier.</param>
public class ReactorAlreadyRegistered(ReactorId id) : Exception($"Reactor '{id}' is already registered in this client.");
