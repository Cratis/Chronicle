// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// The exception that is thrown when an argument for a reactor handler method cannot be resolved because it is
/// neither the event, the <see cref="Events.EventContext"/>, a known read model, nor available from the service provider.
/// </summary>
/// <param name="parameterType">The <see cref="Type"/> of the parameter that could not be resolved.</param>
public class CannotResolveReactorMethodArgument(Type parameterType)
    : Exception($"Could not resolve reactor handler method argument of type '{parameterType.FullName}'. It is not a read model and no service provider was available to resolve it from.");
