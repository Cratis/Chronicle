// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Aksio.Cratis.Changes;

namespace Aksio.Cratis.Integration.for_ImportBuilderExtensions.given;

public class a_change_on_a_nested_property : Specification
{
    protected IImportBuilderFor<ComplexModel, ExternalModel> import_builder;
    protected Subject<ImportContext<ComplexModel, ExternalModel>> subject;
    protected IObservable<ImportContext<ComplexModel, ExternalModel>> context;
    protected Changeset<ComplexModel, ComplexModel> changeset;
    protected EventsToAppend events_to_append;
    protected ComplexModel original_model;
    protected ComplexModel modified_model;
    protected Mock<IObjectComparer> objects_comparer;

    void Establish()
    {
        subject = new Subject<ImportContext<ComplexModel, ExternalModel>>();
        import_builder = new ImportBuilderFor<ComplexModel, ExternalModel>(subject);
        objects_comparer = new();
        objects_comparer
            .Setup(_ => _.Equals(original_model, modified_model, out Ref<IEnumerable<PropertyDifference>>.IsAny))
            .Returns((object? _, object? __, out IEnumerable<PropertyDifference> differences) =>
            {
                differences = new[]
                {
                        new PropertyDifference(new($"{nameof(ComplexModel.Child)}.{nameof(Model.SomeInteger)}"), 43, 44)
                };

                return false;
            });
        original_model = new(42, "Forty Two", new(43, "Forty Three", "Three"));
        modified_model = new(42, "Forty Two", new(44, "Forty Three", "Four"));
        changeset = new(objects_comparer.Object, modified_model, original_model);
        changeset.Add(new PropertiesChanged<ComplexModel>(modified_model, new[]
        {
                new PropertyDifference(new($"{nameof(ComplexModel.Child)}.{nameof(Model.SomeInteger)}"), 43, 44)
        }));

        events_to_append = new();
    }
}
