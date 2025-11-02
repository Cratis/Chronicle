// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// The exception that is thrown when both EventStreamIdAttribute with a value and ICanProvideEventStreamId are applied to a command.
/// </summary>
/// <param name="commandType">Type of command that has ambiguous event stream id.</param>
public class AmbiguousEventStreamId(Type commandType) : Exception($"Command '{commandType.Name}' has both EventStreamIdAttribute with a value and implements ICanProvideEventStreamId. Please use only one method to provide the event stream id, or set the attribute value to null to use the interface.");
