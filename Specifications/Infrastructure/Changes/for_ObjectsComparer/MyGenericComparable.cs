// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Changes.for_ObjectComparer;

#pragma warning disable CS0144, CA1036
public class MyGenericComparable(int desiredResult) : IComparable<MyGenericComparable>
{
    public int CompareTo(MyGenericComparable other) => desiredResult;
}
