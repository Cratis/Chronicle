// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Integration.for_ImportOperations.given;

public class no_changes : all_dependencies_for<SomeEvent>
{
    protected Model _initial;
    protected ExternalModel _incoming;
    protected ImportOperations<Model, ExternalModel> _operations;

    void Establish()
    {
        _initial = new(42, "Forty Two", "Two");
        _incoming = new(42, "Forty Two");

        _projection.GetById(key).Returns(Task.FromResult(new AdapterProjectionResult<Model>(_initial, [], 0)));
        _mapper.Map<Model>(_incoming).Returns(_initial);
        _objectsComparer.Compare(_initial, Arg.Any<Model>(), out Arg.Any<IEnumerable<PropertyDifference>>()).Returns(true);

        _operations = new(
            _adapter,
            _projection,
            _mapper,
            _objectsComparer,
            _eventLog,
            causation_manager);
    }
}
