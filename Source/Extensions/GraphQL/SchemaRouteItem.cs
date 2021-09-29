// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Extensions.GraphQL
{
    public class SchemaRouteItem
    {
        public SchemaRouteItem(MethodInfo method, string name)
        {
            Method = method;
            Name = name;
        }

        public MethodInfo Method { get; }
        public string Name { get; }
    }
}
