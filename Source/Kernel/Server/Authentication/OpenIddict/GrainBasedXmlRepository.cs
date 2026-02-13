// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Chronicle.Grains.Security;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict;

/// <summary>
/// Represents an XML repository for Data Protection keys that uses an Orleans grain for storage.
/// This ensures consistent key management across cluster nodes.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for accessing grains.</param>
public class GrainBasedXmlRepository(IGrainFactory grainFactory) : IXmlRepository
{
    const string GrainId = "chronicle-data-protection";

    /// <inheritdoc/>
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var grain = grainFactory.GetGrain<IDataProtectionKeys>(GrainId);
        var xmlStrings = grain.GetAllKeys().GetAwaiter().GetResult();
        return xmlStrings.Select(XElement.Parse).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public void StoreElement(XElement element, string friendlyName)
    {
        var grain = grainFactory.GetGrain<IDataProtectionKeys>(GrainId);
        grain.StoreKey(friendlyName, element.ToString()).GetAwaiter().GetResult();
    }
}
