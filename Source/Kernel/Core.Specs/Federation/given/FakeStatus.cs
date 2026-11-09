// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A fake status that keeps every binding status written to it.
/// </summary>
public class FakeStatus : IFederationStatus
{
    readonly Dictionary<string, FederationBindingStatus> _bindings = [];

    public List<FederationBindingStatus> History { get; } = [];

    public void RecordPull(string linkId, int events, long bytes)
    {
    }

    public void RecordThrottled(string linkId)
    {
    }

    public FederationLinkStatus GetLink(string linkId) => throw new NotSupportedException();

    public void SetBinding(string bindingId, FederationBindingStatus status)
    {
        _bindings[bindingId] = status;
        History.Add(status);
    }

    public FederationBindingStatus GetBinding(string bindingId) =>
        _bindings.TryGetValue(bindingId, out var status) ? status : FederationBindingStatus.Unknown;
}
