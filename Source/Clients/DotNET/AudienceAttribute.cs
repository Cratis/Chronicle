// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Marks the audience an event is addressed to.
/// </summary>
/// <remarks>
/// On an event type the audience is required: appending the event without an 'audience' tag (from the property
/// below, an <see cref="IEventTagEnricher"/>, <see cref="EventTags.Audience(string)"/> or the call site) fails with
/// <see cref="MissingAudience"/>. On a property (or record parameter) of the event its value is stamped on the event as the
/// 'audience' named tag; a property does not by itself make the audience required.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class AudienceAttribute : Attribute;
