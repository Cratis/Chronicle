// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// The result of probing Chronicle's required MongoDB features.
/// </summary>
/// <param name="ChangeStreams">The change-stream outcome.</param>
/// <param name="Collation">The collation outcome.</param>
public record MongoDBCapabilityReport(MongoDBCapability ChangeStreams, MongoDBCapability Collation)
{
    /// <summary>
    /// Gets the named capability outcomes.
    /// </summary>
    public IEnumerable<(string Name, MongoDBCapability Result)> Capabilities => [("change streams", ChangeStreams), ("collation", Collation)];

    /// <summary>
    /// Gets only features explicitly rejected by the server, excluding inconclusive probes.
    /// </summary>
    public IEnumerable<string> Failures => Capabilities.Where(capability => capability.Result.State == MongoDBCapabilityState.Unsupported).Select(capability => capability.Name);
}
