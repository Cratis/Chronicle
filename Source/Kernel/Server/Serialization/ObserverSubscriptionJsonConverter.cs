// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Json;
using Cratis.Kernel.Grains.Observation;

namespace Cratis.Kernel.Server.Serialization;

/// <summary>
/// Represents the <see cref="JsonConverter{T}"/> that can convert <see cref="ObserverSubscription"/>.
/// </summary>
public class ObserverSubscriptionJsonConverter : TypeWithObjectPropertiesJsonConverter<ObserverSubscription>
{
    /// <inheritdoc/>
    protected override IEnumerable<string> ObjectProperties => [nameof(ObserverSubscription.Arguments)];
}
