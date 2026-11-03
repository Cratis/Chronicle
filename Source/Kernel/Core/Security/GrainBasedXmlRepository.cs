// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents an XML repository for Data Protection keys that uses an Orleans grain for storage.
/// This ensures consistent key management across cluster nodes.
/// </summary>
/// <remarks>
/// Data Protection reads and writes its key ring synchronously, and it does so wherever a key is first needed - including
/// inside a grain turn, such as when a node signs a kernel event with a key another node created. Blocking on a grain
/// call from inside a turn deadlocks: the outgoing call's filters continue on the calling activation's scheduler, which
/// the blocked thread holds. The grain is therefore called from the thread pool, outside any grain context.
/// </remarks>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for accessing grains.</param>
public class GrainBasedXmlRepository(IGrainFactory grainFactory) : IXmlRepository
{
    const string GrainId = "chronicle-data-protection";

    /// <inheritdoc/>
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var grain = grainFactory.GetGrain<IDataProtectionKeys>(GrainId);
        var xmlStrings = OutsideGrainTurn(grain.GetAllKeys);
        return xmlStrings.Select(XElement.Parse).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public void StoreElement(XElement element, string friendlyName)
    {
        var grain = grainFactory.GetGrain<IDataProtectionKeys>(GrainId);
        var xml = element.ToString();
        OutsideGrainTurn(async () =>
        {
            await grain.StoreKey(friendlyName, xml);
            return true;
        });
    }

    static T OutsideGrainTurn<T>(Func<Task<T>> call) => Task.Run(call).GetAwaiter().GetResult();
}
