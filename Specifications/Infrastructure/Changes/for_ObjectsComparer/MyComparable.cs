// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Changes.for_ObjectComparer;
#pragma warning disable CS0144, CA1036
public class MyComparable : IComparable
{
    readonly int _desiredResult;

    public MyComparable(int desiredResult) => _desiredResult = desiredResult;

    public int CompareTo(object obj) => _desiredResult;
}
