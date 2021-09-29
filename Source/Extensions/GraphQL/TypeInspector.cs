// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace Cratis.Extensions.GraphQL
{
    public class TypeInspector : DefaultTypeInspector
    {
        public override IEnumerable<MemberInfo> GetMembers(Type type)
        {
            var members = base.GetMembers(type);
            return members.Where(_ => _ is PropertyInfo property && property.CanWrite);
        }
    }
}
