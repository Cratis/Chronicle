// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>Discovers Core-owned models for services whose transport implementations remain hand-written.</summary>
/// <param name="assembly">The Core assembly loaded in the generator's isolated context.</param>
public class WireContractDiscovery(Assembly assembly)
{
    /// <summary>Discover explicitly opted-in models without deriving or replacing their legacy service interfaces.</summary>
    /// <returns>The models to register with the normal shared-type generator.</returns>
    public IReadOnlyList<Type> Discover()
    {
        IEnumerable<Type> types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types.OfType<Type>();
        }

        return types.Where(type => type.GetCustomAttributesData().Any(attribute =>
            attribute.AttributeType.FullName == "Cratis.Chronicle.Services.WireContractAttribute")).ToArray();
    }
}
