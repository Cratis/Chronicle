// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

/// <summary>
/// Thrown by the reactor that always fails.
/// </summary>
/// <param name="message">The message of the failure.</param>
public class UnseenReactorFailed(string message) : Exception(message);
