// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Integration.for_ImportBuilderExtensions.given;

public class a_change_on_one_property : no_changes
{
    void Establish()
    {
        _originalModel = new Model(42, "Forty Two", "Two");
        _modifiedModel = new Model(42, "Forty Three", "Three");

        _changeset.Add(new PropertiesChanged<Model>(_modifiedModel,
        [
                new PropertyDifference(
                    new(nameof(Model.SomeString)),
                    _originalModel.SomeString,
                    _modifiedModel.SomeString)
        ]));
    }
}
