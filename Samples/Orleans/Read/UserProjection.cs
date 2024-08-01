using Cratis.Chronicle.Projections;
using Events.Groups;
using Events.Users;

namespace Read.Users;

public class UserProjection : IProjectionFor<User>
{
    public void Define(IProjectionBuilderFor<User> builder) => builder
        .From<OnboardingStarted>(b => b
            .Set(m => m.Name).To(e => e.Name)
            .Set(m => m.UserName).To(e => e.UserName)
            .Set(m => m.Password).To(e => e.Password))
        .From<SystemUserAdded>(b => b
            .Set(m => m.Name).To(e => e.Name)
            .Set(m => m.UserName).To(e => e.UserName)
            .Set(m => m.Password).To(e => e.Password)
            .Set(m => m.System).ToValue(true))
        .From<OnboardingCompleted>(b => b
            .Set(m => m.Onboarded).ToValue(true))
        .From<UserNameChanged>(b => b
            .Set(m => m.UserName).To(e => e.UserName))
        .From<ProfileNameChanged>(b => b
            .Set(m => m.Name).To(e => e.Name))
        .From<PasswordChanged>(b => b
            .Set(m => m.Password).To(e => e.Password))
        .RemovedWith<UserRemoved>()
        .Children(c => c.Groups, b => b
            .IdentifiedBy(e => e.GroupId)
            .From<UserAddedToGroup>(b => b
                .UsingParentKey(e => e.UserId)
                .Set(m => m.GroupId).ToEventSourceId())
            .Join<GroupAdded>(b => b
                .On(m => m.GroupId)
                .Set(m => m.Name).To(e => e.Name)));
}
