// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Integration.for_ImportBuilderExtensions.given;

public class no_changes : Specification
{
    protected IImportBuilderFor<Model, ExternalModel> _importBuilder;
    protected Subject<ImportContext<Model, ExternalModel>> _subject;
    protected IObservable<ImportContext<Model, ExternalModel>> _context;
    protected Changeset<Model, Model> _changeset;
    protected EventsToAppend _eventsToAppend;
    protected Model _originalModel;
    protected Model _modifiedModel;
    protected IObjectComparer _objectsComparer;

    void Establish()
    {
        _subject = new Subject<ImportContext<Model, ExternalModel>>();
        _importBuilder = new ImportBuilderFor<Model, ExternalModel>(_subject);
        _modifiedModel = new Model(42, "Forty Two", "Two");
        _originalModel = new Model(42, "Forty Two", "Two");
        _objectsComparer = Substitute.For<IObjectComparer>();
        _objectsComparer.Compare(_originalModel, _modifiedModel, out Arg.Any<IEnumerable<PropertyDifference>>()).Returns(true);
        _changeset = new(_objectsComparer, _modifiedModel, _originalModel);
        _eventsToAppend = [];
    }
}
