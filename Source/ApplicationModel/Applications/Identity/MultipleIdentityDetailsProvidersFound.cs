// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Applications.Identity;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Exception that gets thrown when there are <see cref="IProvideIdentityDetails">multiple identity details providers</see>.
/// </summary>
public class MultipleIdentityDetailsProvidersFound : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultipleIdentityDetailsProvidersFound"/> class.
    /// </summary>
    /// <param name="types">Types that were found.</param>
    public MultipleIdentityDetailsProvidersFound(IEnumerable<Type> types) : base($"There should only be one implementation of `{nameof(IProvideIdentityDetails)}` found {string.Join(',', types.Select(_ => _.FullName))}")
    {
    }
}
