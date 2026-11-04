// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.for_Projections.given;

public class projections_with_competing_ids : projections_for_explicit_registration
{
    protected const string SharedId = "shared-id";

    void Establish()
    {
        _artifactsActivator.ActivateNonDisposable<IProjectionFor<FirstModel>>(typeof(FirstFluent)).Returns(new FirstFluent());
        _artifactsActivator.ActivateNonDisposable<IProjectionFor<SecondModel>>(typeof(SecondFluent)).Returns(new SecondFluent());
    }

    [Projection(SharedId)]
    [FromEvent<CustomerRegistered>]
    public record FirstModel(string Name);

    [Projection(SharedId)]
    [FromEvent<CustomerRegistered>]
    public record SecondModel(string Name);

    [Projection(SharedId)]
    public class FirstFluent : IProjectionFor<FirstModel>
    {
        public void Define(IProjectionBuilderFor<FirstModel> builder) => builder.From<CustomerRegistered>();
    }

    [Projection(SharedId)]
    public class SecondFluent : IProjectionFor<SecondModel>
    {
        public void Define(IProjectionBuilderFor<SecondModel> builder) => builder.From<CustomerRegistered>();
    }
}
