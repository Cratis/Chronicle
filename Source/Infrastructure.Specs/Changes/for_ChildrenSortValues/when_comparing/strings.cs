// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Changes.for_ChildrenSortValues.when_comparing;

public class strings : Specification
{
    CultureInfo _originalCulture;
    int _upperBeforeLower;
    int _accentedAfterLaterLetter;

    void Establish()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
    }

    void Because()
    {
        _upperBeforeLower = ChildrenSortValues.Compare("B", "a");
        _accentedAfterLaterLetter = ChildrenSortValues.Compare("ä", "b");
    }

    void Destroy() => CultureInfo.CurrentCulture = _originalCulture;

    [Fact] void should_order_by_ordinal_value_not_by_culture() => _upperBeforeLower.ShouldBeLessThan(0);
    [Fact] void should_order_characters_by_their_code_point() => _accentedAfterLaterLetter.ShouldBeGreaterThan(0);
}
