// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event for opening a module — carries module identity plus customer fields used by a nested record.
/// </summary>
/// <param name="Name">Module name.</param>
/// <param name="CustomerName">Name of the customer the module is opened for.</param>
/// <param name="CustomerEmail">Email of the customer the module is opened for.</param>
[EventType]
public record ModuleOpened(string Name, string CustomerName, string CustomerEmail);
