// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Events.Constraints.for_ConstraintValidationContext.given;

public class two_validators_that_are_index_updaters : Specification
{
    protected IConstraintValidator _firstValidator;
    protected IHaveUpdateConstraintIndex _firstValidatorIndexUpdaterProvider;
    protected IUpdateConstraintIndex _firstValidatorIndexUpdater;
    protected IConstraintValidator _secondValidator;
    protected IHaveUpdateConstraintIndex _secondValidatorIndexUpdaterProvider;
    protected IUpdateConstraintIndex _secondValidatorIndexUpdater;
    protected EventSourceId _eventSourceId;
    protected EventType _eventType;
    protected ExpandoObject _content;

    void Establish()
    {
        _firstValidator = Substitute.For<IConstraintValidator, IHaveUpdateConstraintIndex>();
        _firstValidatorIndexUpdaterProvider = _firstValidator as IHaveUpdateConstraintIndex;
        _firstValidatorIndexUpdater = Substitute.For<IUpdateConstraintIndex>();
        _firstValidatorIndexUpdaterProvider.GetUpdateFor(Arg.Any<ConstraintValidationContext>()).Returns(_firstValidatorIndexUpdater);

        _secondValidator = Substitute.For<IConstraintValidator, IHaveUpdateConstraintIndex>();
        _secondValidatorIndexUpdaterProvider = _secondValidator as IHaveUpdateConstraintIndex;
        _secondValidatorIndexUpdater = Substitute.For<IUpdateConstraintIndex>();
        _secondValidatorIndexUpdaterProvider.GetUpdateFor(Arg.Any<ConstraintValidationContext>()).Returns(_secondValidatorIndexUpdater);

        _eventSourceId = EventSourceId.New();
        _eventType = new("SomeEvent", 1);
        _content = new();
    }
}
