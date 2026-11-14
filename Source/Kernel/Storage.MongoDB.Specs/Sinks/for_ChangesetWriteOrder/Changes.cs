// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetWriteOrder;

static class Changes
{
    public static PropertiesChanged<ExpandoObject> Direct(params string[] properties) =>
        new(new ExpandoObject(), properties.Select(property => new PropertyDifference(new PropertyPath(property), 0L, 1L)).ToArray());

    public static Joined Join(bool hasKeyedFrom, params string[] properties) =>
        new(new ExpandoObject(), "key", new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [Direct(properties)])
        {
            HasKeyedFrom = hasKeyedFrom
        };
}
