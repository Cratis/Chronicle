// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Represents an implementation of <see cref="IImmediatePeerFeature"/>.
/// </summary>
/// <param name="Address">The address of the immediate peer.</param>
public record ImmediatePeerFeature(IPAddress? Address) : IImmediatePeerFeature;
