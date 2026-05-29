// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Exception that gets thrown when a reactor handler method has an unsupported return type.
/// </summary>
/// <param name="reactorType">The <see cref="Type"/> of the reactor.</param>
/// <param name="methodName">The name of the invalid handler method.</param>
/// <param name="actualReturnType">The actual return <see cref="Type"/>.</param>
public class InvalidReactorHandlerReturnType(Type reactorType, string methodName, Type actualReturnType)
    : Exception(
        $"Reactor '{reactorType.FullName}' handler method '{methodName}' has unsupported return type '{actualReturnType.FullName}'. " +
        $"Expected 'void', '{typeof(Task).FullName}', '{typeof(Task<>).FullName}' returning an event, " +
        $"or '{typeof(Task<>).FullName}' returning '{typeof(IEnumerable<>).FullName}' of events. " +
        $"Synchronous equivalents are also supported: an event type, or '{typeof(IEnumerable<>).FullName}' of events.");