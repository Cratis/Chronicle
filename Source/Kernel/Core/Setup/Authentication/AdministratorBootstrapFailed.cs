// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Authentication;

/// <summary>
/// The exception that is thrown when the initial administrator could not be persisted.
/// </summary>
/// <param name="operation">The bootstrap operation that failed.</param>
public class AdministratorBootstrapFailed(string operation) :
    Exception($"Administrator bootstrap failed while attempting to {operation}.");
