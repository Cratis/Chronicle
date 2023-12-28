// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Grains.Observation;

namespace Aksio.Cratis.Kernel.Server.Serialization;

/// <summary>
/// Represents the <see cref="JsonConverter{T}"/> that can convert <see cref="ObserverSubscription"/>.
/// </summary>
public class ObserverSubscriptionJsonConverter : TypeWithObjectPropertiesJsonConverter<ObserverSubscription>
{
    /// <inheritdoc/>
    protected override IEnumerable<string> ObjectProperties => new[]
    {
        nameof(ObserverSubscription.Arguments)
    };
}
