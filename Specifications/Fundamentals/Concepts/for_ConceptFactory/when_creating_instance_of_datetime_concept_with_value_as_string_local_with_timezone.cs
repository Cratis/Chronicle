// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Aksio.Cratis.Concepts.for_ConceptFactory;

public class when_creating_instance_of_datetime_concept_with_value_as_string_local_with_timezone : Specification
{
    DateTimeConcept result;
    string now;

    void Establish()
    {
        Console.WriteLine($"CUlture: '{CultureInfo.CurrentCulture.ThreeLetterISOLanguageName}'");
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        now = "2022-12-14T09:45:46.4595800+01:00";
    }

    void Because() => result = ConceptFactory.CreateConceptInstance(typeof(DateTimeConcept), now) as DateTimeConcept;

    [Fact] void should_be_the_value_of_the_datetime() => result.Value.ToString("o").ShouldEqual(now);
}
