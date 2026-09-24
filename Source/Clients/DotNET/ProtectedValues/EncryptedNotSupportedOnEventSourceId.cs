// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// The exception that is thrown when the <see cref="EncryptedAttribute"/> is applied to a type that inherits from
/// <see cref="Events.EventSourceId"/> or <see cref="Events.EventSourceId{T}"/>.
/// </summary>
/// <remarks>
/// Event source identifiers are used to correlate events and cannot be encrypted, because encrypting them would
/// break the ability to look up and correlate events. If the identifier itself is a secret, consider using a
/// non-sensitive surrogate key as the event source identifier and storing the secret in a separate event field
/// marked with <see cref="EncryptedAttribute"/>.
/// </remarks>
/// <param name="type">The <see cref="Type"/> that has the attribute applied incorrectly.</param>
public class EncryptedNotSupportedOnEventSourceId(Type type)
    : Exception($"The [Encrypted] attribute cannot be applied to '{type.FullName}' because it inherits from EventSourceId or EventSourceId<T>. Event source identifiers cannot be encrypted as they are required for event correlation.");
