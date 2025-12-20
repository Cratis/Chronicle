// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Exception that gets thrown when no event source id is found in the command context values.
/// </summary>
/// <param name="commandType">The command type that was being processed.</param>
public class MissingEventSourceIdInCommandContext(Type commandType)
    : Exception($"No event source id found in command context for command of type '{commandType.FullName}'. Ensure that a {nameof(ICommandContextValuesProvider)} is registered that provides the event source id.")
{
}
