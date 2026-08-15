// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event that writes the nested summary on a <see cref="FluentProjectNotes"/>.
/// </summary>
/// <param name="Headline">The summary headline.</param>
/// <param name="Note">The summary note.</param>
[EventType]
public record FluentProjectSummarised(string Headline, string Note);
