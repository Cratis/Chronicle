// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Integration.for_ImportBuilderExtensions.given;

public class a_change_on_a_nested_property : Specification
{
    protected IImportBuilderFor<ComplexModel, ExternalModel> _importBuilder;
    protected Subject<ImportContext<ComplexModel, ExternalModel>> _subject;
    protected IObservable<ImportContext<ComplexModel, ExternalModel>> _context;
    protected Changeset<ComplexModel, ComplexModel> _changeset;
    protected EventsToAppend _eventsToAppend;
    protected ComplexModel _originalModel;
    protected ComplexModel _modifiedModel;
    protected IObjectComparer _objectsComparer;

    void Establish()
    {
        _subject = new Subject<ImportContext<ComplexModel, ExternalModel>>();
        _importBuilder = new ImportBuilderFor<ComplexModel, ExternalModel>(_subject);
        _objectsComparer = Substitute.For<IObjectComparer>();
        _objectsComparer
            .Compare(_originalModel, _modifiedModel, out Arg.Any<IEnumerable<PropertyDifference>>())
            .Returns(callInfo =>
            {
                var differences = callInfo.Arg<IEnumerable<PropertyDifference>>();
                differences =
                [
                        new PropertyDifference(new($"{nameof(ComplexModel.Child)}.{nameof(Model.SomeInteger)}"), 43, 44)
                ];

                return false;
            });
        _originalModel = new(42, "Forty Two", new(43, "Forty Three", "Three"));
        _modifiedModel = new(42, "Forty Two", new(44, "Forty Three", "Four"));
        _changeset = new(_objectsComparer, _modifiedModel, _originalModel);
        _changeset.Add(new PropertiesChanged<ComplexModel>(_modifiedModel,
        [
                new PropertyDifference(new($"{nameof(ComplexModel.Child)}.{nameof(Model.SomeInteger)}"), 43, 44)
        ]));

        _eventsToAppend = [];
    }
}
