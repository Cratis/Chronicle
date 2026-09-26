// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// The exception that is thrown when no side-effect handler can process a reactor's returned value.
/// </summary>
/// <param name="reactorType">The reactor that returned the value.</param>
/// <param name="returnType">The type of the value that could not be handled.</param>
public class UnhandledReactorReturnValue(Type reactorType, Type returnType)
    : Exception($"Reactor '{reactorType.FullName}' returned a value of type '{returnType.FullName}' that no side-effect handler can process.");
