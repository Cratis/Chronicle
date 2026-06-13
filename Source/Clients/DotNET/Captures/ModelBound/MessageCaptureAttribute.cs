// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.ModelBound;

/// <summary>
/// Attribute used to configure a capture that observes a message topic.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="MessageCaptureAttribute"/>.
/// </remarks>
/// <param name="topic">The topic to observe.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class MessageCaptureAttribute(string topic) : Attribute
{
    /// <summary>
    /// Gets the topic to observe.
    /// </summary>
    public string Topic { get; } = topic;
}
