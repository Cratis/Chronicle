// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace Cratis.Extensions.GraphQL
{
    public class NamingConventions : DefaultNamingConventions
    {
        public override NameString GetTypeName(Type type, TypeKind kind)
        {
            var name = base.GetTypeName(type, kind);
            if (kind == TypeKind.Enum &&
                !type.Namespace!.StartsWith("HotChocolate", StringComparison.InvariantCultureIgnoreCase) &&
                !type.Namespace!.StartsWith("Cratis", StringComparison.InvariantCultureIgnoreCase) &&
                !name.Value.EndsWith("Enum", StringComparison.InvariantCultureIgnoreCase))
            {
                name = new NameString($"{name.Value}Enum");
            }

            return name;
        }
    }
}
