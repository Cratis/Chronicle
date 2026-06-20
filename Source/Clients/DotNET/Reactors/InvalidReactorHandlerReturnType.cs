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
        "A handler method must return 'void', 'Task', an event type, 'EventForEventSourceId', an 'IEnumerable' of those, " +
        "or any of these wrapped in a 'Task'. A custom side-effect type processed by an 'IReactorSideEffectHandler' must be returned as 'Task<T>'.");