// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_BackgroundFaults;

public class when_describing_the_owner : Specification
{
    string _owner;

    void Because()
    {
        try
        {
            ThrowFromOwner();
        }
        catch (Exception ex)
        {
            _owner = BackgroundFaults.DescribeOwner(new AggregateException(ex));
        }
    }

    [Fact] void should_name_the_type_and_method_that_threw() => _owner.ShouldContain(nameof(ThrowFromOwner));

    static void ThrowFromOwner() => throw new TimeoutException();
}
