// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Exception that gets thrown when a CLR type does not have an event type registered.
/// </summary>
public class MissingEventTypeForClrType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingEventTypeForClrType"/> class.
    /// </summary>
    /// <param name="type">CLR type that is missing event type.</param>
    public MissingEventTypeForClrType(Type type) : base($"Missing event type definition for CLR type '{type.FullName}'. Has it been adorned with an [EventType(\"<guid>\")] attribute?")
    {
    }
}
