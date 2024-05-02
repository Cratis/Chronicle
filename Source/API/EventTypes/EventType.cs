// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.EventTypes;


/// <summary>
/// Represents the type of an event.
/// </summary>
/// <param name="Id">The unique identifier of the event.</param>
/// <param name="Generation">The generation of the event.</param>
/// <param name="IsPublic">Whether or not the event type is considered a public event.</param>
public record EventType(string Id, uint Generation, bool IsPublic = false);
