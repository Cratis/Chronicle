using Cratis.Chronicle.Events.Constraints;

namespace Events.Users;

public class UniqueUserNameConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .Unique(_ => _
            .On<OnboardingStarted>(_ => _.UserName)
            .On<UserNameChanged>(_ => _.UserName);
}
