// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Execution
{
    /// <summary>
    /// Attribute to adorn types for the IoC hookup to recognize it as a Singleton.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SingletonPerTenantAttribute : Attribute
    {
    }
}
