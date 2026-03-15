// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace TestApp;

/// <summary>
/// Generation 1 of <see cref="MyEvent"/> — carries a single combined message in the form "subject:body".
/// </summary>
/// <param name="Message">The raw message string, formatted as "subject:body".</param>
[EventType("my-event", generation: 1)]
public record MyEventV1(string Message);

/// <summary>
/// Generation 2 of MyEvent — the Message property has been split into separate Subject and Body fields.
/// </summary>
/// <param name="Subject">The subject part (everything before the first colon).</param>
/// <param name="Body">The body part (everything after the first colon).</param>
[EventType("my-event", generation: 2)]
public record MyEvent(string Subject, string Body);


