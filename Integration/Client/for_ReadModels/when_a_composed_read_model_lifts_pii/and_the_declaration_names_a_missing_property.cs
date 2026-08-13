// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii.and_the_declaration_names_a_missing_property.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii;

/// <summary>
/// A declaration naming a property the row does not have. Falling back to the row's own subject would put
/// the value straight back into one of the two silent outcomes the declaration was written to avoid, so the
/// read fails and names what is wrong instead.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_declaration_names_a_missing_property(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.two_stored_subjects(fixture)
    {
        public Exception? Error { get; private set; }

        async Task Because()
        {
            await StoreBothSubjects();
            Error = await Catch.Exception(() => EventStore.ReadModels.Release(new MisdeclaredDueSubject(FirstPerson.Value, "Awaiting counsel from the family")));
        }
    }

    [Fact] void should_fail() => Context.Error.ShouldBeOfExactType<ReleaseUnderPropertyNotFound>();
    [Fact] void should_name_the_property_it_points_at() => ((ReleaseUnderPropertyNotFound)Context.Error!).SubjectPropertyName.ShouldEqual("PersonId");
}

/// <summary>
/// A composed row whose declaration points at a property it does not have.
/// </summary>
/// <param name="SubjectId">The person the row is about.</param>
/// <param name="Comment">The comment lifted off that person's own stored row.</param>
public record MisdeclaredDueSubject(
    SubjectIdentifier SubjectId,
    [SubjectFrom("PersonId")] PostponementComment Comment);

#pragma warning restore SA1402
