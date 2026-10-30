// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Sys;
using Orleans.Metadata;

namespace Cratis.Chronicle.Releases;

/// <summary>
/// Publishes this kernel's release using interface properties understood by older Orleans silos.
/// </summary>
internal sealed class KernelReleaseInterfaceProperties : IGrainInterfacePropertiesProvider
{
    /// <summary>
    /// The additive manifest property containing a silo's kernel release.
    /// </summary>
    internal const string ReleaseKey = "chronicle.kernel-release";

    /// <inheritdoc/>
    public void Populate(Type interfaceType, GrainInterfaceType grainInterfaceType, Dictionary<string, string> properties)
    {
        if (interfaceType == typeof(IObserver))
        {
            properties[ReleaseKey] = KernelVersion.Current.ToString();
        }
    }
}
