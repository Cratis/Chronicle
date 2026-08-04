// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Registrations.for_RegistrationWaitExtensions.given;

public class an_event_store_whose_registration_is_observed : Specification
{
    protected static readonly RegistrationOutcome _ran = new(true, ImmutableList.Create(new ArtifactRegistration(typeof(a_read_model), null)));

    protected IEventStore _eventStore;
    protected int _timesObserved;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Registration.Returns(_ => Observe());
    }

    protected virtual RegistrationOutcome Observe()
    {
        _timesObserved++;
        return _ran;
    }

    public record a_read_model();
}
