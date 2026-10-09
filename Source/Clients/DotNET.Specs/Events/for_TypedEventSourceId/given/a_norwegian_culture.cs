// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.given;

public class a_norwegian_culture : Specification
{
    CultureInfo _originalCulture;
    CultureInfo _originalUICulture;

    void Establish()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        _originalUICulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nb-NO");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("nb-NO");
    }

    void Destroy()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        CultureInfo.CurrentUICulture = _originalUICulture;
    }
}
