// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the fluent builder for a capture.
/// </summary>
public interface ICaptureBuilder
{
    /// <summary>
    /// Configures the capture to observe an API source.
    /// </summary>
    /// <param name="api">The API name to observe.</param>
    /// <param name="configure">Optional callback for further source configuration.</param>
    /// <returns>The builder continuation.</returns>
    ICaptureBuilder FromApi(string api, Action<IApiSourceBuilder>? configure = null);

    /// <summary>
    /// Configures the capture to observe a webhook source.
    /// </summary>
    /// <param name="path">The webhook path.</param>
    /// <param name="configure">Optional callback for further source configuration.</param>
    /// <returns>The builder continuation.</returns>
    ICaptureBuilder FromWebhook(string path, Action<IWebhookSourceBuilder>? configure = null);

    /// <summary>
    /// Configures the capture to observe a message topic.
    /// </summary>
    /// <param name="topic">The topic to observe.</param>
    /// <returns>The builder continuation.</returns>
    ICaptureBuilder FromMessageTopic(string topic);

    /// <summary>
    /// Sets the property path used as the capture key.
    /// </summary>
    /// <param name="propertyPath">The property path.</param>
    /// <returns>The builder continuation.</returns>
    ICaptureBuilder Key(string propertyPath);

    /// <summary>
    /// Configures map operations for the root capture scope.
    /// </summary>
    /// <param name="configure">Callback for configuring the map.</param>
    /// <returns>The builder continuation.</returns>
    ICaptureBuilder Map(Action<IMapBuilder> configure);

    /// <summary>
    /// Appends an event when the configured condition matches.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to append.</typeparam>
    /// <param name="configure">Callback for configuring the append definition.</param>
    /// <returns>The builder continuation.</returns>
    ICaptureBuilder Append<TEvent>(Action<IAppendBuilder<TEvent>> configure)
        where TEvent : class;

    /// <summary>
    /// Configures a nested object scope.
    /// </summary>
    /// <param name="objectPath">The nested object path.</param>
    /// <param name="configure">Callback for configuring the nested scope.</param>
    /// <returns>The builder continuation.</returns>
    ICaptureBuilder Nested(string objectPath, Action<INestedCaptureBuilder> configure);

    /// <summary>
    /// Configures a child collection scope.
    /// </summary>
    /// <param name="collectionPath">The child collection path.</param>
    /// <param name="identifiedBy">The property identifying a child item.</param>
    /// <param name="configure">Callback for configuring the child scope.</param>
    /// <returns>The builder continuation.</returns>
    ICaptureBuilder Children(string collectionPath, string identifiedBy, Action<IChildrenCaptureBuilder> configure);
}
