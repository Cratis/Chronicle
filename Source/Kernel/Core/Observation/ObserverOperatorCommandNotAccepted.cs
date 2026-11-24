// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the exception that occurs when the jobs manager did not accept a command an operator gave an observer.
/// </summary>
/// <param name="command">The <see cref="ObserverOperatorCommand"/> that was not accepted.</param>
/// <param name="observer">The <see cref="ObserverKey"/> of the observer the command was for.</param>
public class ObserverOperatorCommandNotAccepted(ObserverOperatorCommand command, ObserverKey observer) : Exception($"The command '{command}' could not be accepted for observer '{observer}'.");
