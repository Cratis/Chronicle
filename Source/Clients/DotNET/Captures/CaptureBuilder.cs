// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents an implementation of <see cref="ICaptureBuilder"/>.
/// </summary>
public class CaptureBuilder : ICaptureBuilder
{
    readonly List<AppendDefinition> _appends = [];
    readonly List<ChildrenDefinition> _children = [];
    readonly List<NestedDefinition> _nested = [];
    string? _keyProperty;
    MapDefinition? _map;
    SourceDefinition? _source;

    /// <inheritdoc/>
    public ICaptureBuilder FromApi(string api, Action<IApiSourceBuilder>? configure = null)
    {
        var builder = new ApiSourceBuilder(api);
        configure?.Invoke(builder);
        _source = builder.Build();

        return this;
    }

    /// <inheritdoc/>
    public ICaptureBuilder FromWebhook(string path, Action<IWebhookSourceBuilder>? configure = null)
    {
        var builder = new WebhookSourceBuilder(path);
        configure?.Invoke(builder);
        _source = builder.Build();

        return this;
    }

    /// <inheritdoc/>
    public ICaptureBuilder FromMessageTopic(string topic)
    {
        _source = new(SourceType.Message, Topic: topic);

        return this;
    }

    /// <inheritdoc/>
    public ICaptureBuilder Key(string propertyPath)
    {
        _keyProperty = propertyPath;

        return this;
    }

    /// <inheritdoc/>
    public ICaptureBuilder Map(Action<IMapBuilder> configure)
    {
        var builder = new MapBuilder();
        configure(builder);
        _map = builder.Build();

        return this;
    }

    /// <inheritdoc/>
    public ICaptureBuilder Append<TEvent>(Action<IAppendBuilder<TEvent>> configure)
        where TEvent : class
    {
        var builder = new AppendBuilder<TEvent>();
        configure(builder);
        _appends.Add(builder.Build());

        return this;
    }

    /// <inheritdoc/>
    public ICaptureBuilder Nested(string objectPath, Action<INestedCaptureBuilder> configure)
    {
        var builder = new NestedCaptureBuilder();
        configure(builder);
        _nested.Add(builder.Build(objectPath));

        return this;
    }

    /// <inheritdoc/>
    public ICaptureBuilder Children(string collectionPath, string identifiedBy, Action<IChildrenCaptureBuilder> configure)
    {
        var builder = new ChildrenCaptureBuilder();
        configure(builder);
        _children.Add(builder.Build(collectionPath, identifiedBy));

        return this;
    }

    /// <summary>
    /// Builds the <see cref="CaptureDefinition"/>.
    /// </summary>
    /// <returns>A new <see cref="CaptureDefinition"/>.</returns>
    public CaptureDefinition Build()
    {
        ThrowIfSourceIsMissing();
        ThrowIfKeyIsMissing();

        return new(
            CaptureId.New(),
            _source!,
            _keyProperty!,
            _map,
            _appends.ToArray(),
            _nested.ToArray(),
            _children.ToArray());
    }

    void ThrowIfKeyIsMissing()
    {
        if (string.IsNullOrWhiteSpace(_keyProperty))
        {
            throw new MissingCaptureKey();
        }
    }

    void ThrowIfSourceIsMissing()
    {
        if (_source is null)
        {
            throw new MissingCaptureSourceConfiguration();
        }
    }
}
