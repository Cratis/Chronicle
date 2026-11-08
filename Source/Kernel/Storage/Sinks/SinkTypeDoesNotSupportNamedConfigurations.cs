// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The exception that is thrown when a sink factory is asked for a named configuration it cannot connect to.
/// </summary>
/// <param name="type">The <see cref="SinkTypeId"/> of the factory.</param>
/// <param name="name">The <see cref="SinkName"/> asked for.</param>
public class SinkTypeDoesNotSupportNamedConfigurations(SinkTypeId type, SinkName name)
    : Exception($"Sink type '{type}' cannot store to the named sink configuration '{name}'.");
