// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Integration.for_ImportOperations.given;

public class one_property_changed_for<TEvent> : all_dependencies_for<TEvent>
{
    protected Model initial;
    protected Model mapped;
    protected ExternalModel incoming;
    protected ImportOperations<Model, ExternalModel> operations;

    void Establish()
    {
        initial = new(42, "Forty Two", "Two");
        incoming = new(43, "Forty Two");
        mapped = new(incoming.SomeInteger, incoming.SomeString, null!);

        _projection.GetById(key).Returns(Task.FromResult(new AdapterProjectionResult<Model>(initial, [], 0)));
        _mapper.Map<Model>(incoming).Returns(mapped);

        _objectsComparer = Substitute.For<IObjectComparer>();
        _objectsComparer
            .Compare(initial, Arg.Any<Model>(), out Arg.Any<IEnumerable<PropertyDifference>>())
            .Returns(callInfo =>
            {
                callInfo[2] = new List<PropertyDifference>
                {
                    new(new(nameof(Model.SomeInteger)), initial.SomeInteger, incoming.SomeInteger)
                };
                return false;
            });

        operations = new(
            _adapter,
            _projection,
            _mapper,
            _objectsComparer,
            _eventLog,
            causation_manager);
    }
}
