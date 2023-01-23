// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents the information sent to the Kernel when connecting.
/// </summary>
/// <param name="ClientVersion">The version of the client.</param>
/// <param name="AdvertisedUri">The URI that the client is advertised with.</param>
public record ClientInformation(string ClientVersion, string AdvertisedUri);
