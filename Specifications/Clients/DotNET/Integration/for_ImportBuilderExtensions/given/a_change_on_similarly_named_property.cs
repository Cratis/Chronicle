// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Integration.for_ImportBuilderExtensions.given;

public class a_change_on_similarly_named_property : no_changes
{
    void Establish()
    {
        original_model = new Model(42, "Forty Two", "Two");
        modified_model = new Model(42, "Forty Two", "Three");

        changeset.Add(new PropertiesChanged<Model>(modified_model,
        [
            new PropertyDifference(
                new(nameof(Model.SomeString2)),
                original_model.SomeString2,
                modified_model.SomeString2)
        ]));
    }
}
