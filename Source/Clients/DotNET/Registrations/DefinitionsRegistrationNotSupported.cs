// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The exception thrown when an event store does not support definitions registration.
/// </summary>
/// <param name="type">The event store implementation.</param>
public class DefinitionsRegistrationNotSupported(Type type)
    : Exception($"The event store implementation '{type.FullName}' does not support definitions registration.");
