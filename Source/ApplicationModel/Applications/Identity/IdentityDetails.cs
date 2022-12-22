// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.Identity;

/// <summary>
/// Represents the identity details returned by <see cref="IProvideIdentityDetails"/>.
/// </summary>
/// <param name="IsUserAuthorized">Whether or not the user is authorized.</param>
/// <param name="Details">The actual details.</param>
public record IdentityDetails(bool IsUserAuthorized, object Details);
