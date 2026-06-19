// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Recommendations;
using Orleans.Serialization.Configuration;

[assembly: TypeManifestProvider(typeof(SpecsTypeManifestProvider))]

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Represents a <see cref="TypeManifestProviderBase"/> that allows the recommendation request types used in
/// specs through Orleans' type manifest.
/// </summary>
/// <remarks>
/// The production silo allows recommendation request types by registering the Cratis JSON serializer (see
/// SerializationConfigurationExtensions). The OrleansTestKit silo used by specs does not configure that
/// serializer, so Orleans' strict type manifest (enforced as of Orleans 10.2) rejects these types when
/// constructing the generic <c>IRecommendation&lt;TRequest&gt;</c> grain type. Allowing them here restores
/// the behavior for specs without relaxing production security.
/// </remarks>
public class SpecsTypeManifestProvider : TypeManifestProviderBase
{
    /// <inheritdoc/>
    protected override void ConfigureInner(TypeManifestOptions options) =>
        options.AllowAllTypes = true;
}
