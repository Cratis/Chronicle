// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Reflection.for_TypeExtensions;

public class MyEnumerable : IEnumerable<ComplexType>
{
    IEnumerable<ComplexType> _list = new List<ComplexType>();

    public IEnumerator<ComplexType> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _list.GetEnumerator();
    }
}
