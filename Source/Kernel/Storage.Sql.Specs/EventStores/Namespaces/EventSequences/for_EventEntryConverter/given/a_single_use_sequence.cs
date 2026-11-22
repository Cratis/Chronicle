// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter.given;

public sealed class a_single_use_sequence<T>(IEnumerable<T> values) : IEnumerable<T>
{
    public int Enumerations { get; private set; }

    public IEnumerator<T> GetEnumerator()
    {
        if (++Enumerations != 1)
        {
            throw new InvalidOperationException("The sequence was enumerated twice.");
        }

        return values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
