// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects.for_SubjectValuesProvider.when_providing;

public class with_interface_implementation : Specification
{
    SubjectValuesProvider _provider;
    ReactorContextValues _result;
    Subject _subject;

    void Establish()
    {
        _provider = new SubjectValuesProvider();
        _subject = new Subject("my-subject");
    }

    void Because() => _result = _provider.Provide(new ReactorWithSubject(_subject), EventContext.EmptyWithEventSourceId(EventSourceId.New()));

    [Fact] void should_return_subject_value() => _result.ContainsKey(WellKnownReactorContextKeys.Subject).ShouldBeTrue();
    [Fact] void should_have_the_provided_subject() => ((Subject)_result[WellKnownReactorContextKeys.Subject]).ShouldEqual(_subject);

    class ReactorWithSubject(Subject subject) : IReactor, ICanProvideSubject
    {
        public Subject GetSubject() => subject;
    }
}
