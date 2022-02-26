// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Aksio.Cratis.Extensions.Orleans.Configuration;

/// <summary>
/// Represents the configuration of an <see cref="IPEndPoint"/>.
/// </summary>
/// <param name="Address">The address.</param>
/// <param name="Port">The port.</param>
public record EndPoint(string Address, string Port);
