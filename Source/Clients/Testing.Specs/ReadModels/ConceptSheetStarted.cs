// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event that starts a <see cref="ConceptSheet"/>.
/// </summary>
/// <param name="Year">The year the sheet covers.</param>
[EventType]
public record ConceptSheetStarted(int Year);
