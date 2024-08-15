using Cratis.Chronicle.Projections;
using Events.Groups;
using Events.Users;

namespace Read.Groups;

public class GroupProjection : IProjectionFor<Group>
{
    public void Define(IProjectionBuilderFor<Group> builder) => builder
        .WithInitialValues(() => new Group())
        .FromEvery(_ => _.Set(m => m.LastUpdated).ToEventContextProperty(_ => _.Occurred))
        .From<SystemGroupAdded>(b => b
            .Set(g => g.Name).To(e => e.Name)
            .Set(g => g.IsSystem).ToValue(true))
        .From<GroupAdded>(b => b
            .Set(g => g.Name).To(e => e.Name))
        .Children(g => g.Users, b => b
            .IdentifiedBy(u => u.UserId)
            .From<UserAddedToGroup>(b => b
                .UsingKey(e => e.UserId))

            // .Join<OnboardingStarted>(b => b
            //     .On(r => r.UserId)
            //     .Set(u => u.UserName).To(e => e.UserName)
            //     .Set(u => u.ProfileName).To(e => e.ProfileName))
            // .Join<SystemUserAdded>(b => b
            //     .On(r => r.UserId)
            //     .Set(u => u.UserName).To(e => e.UserName)
            //     .Set(u => u.ProfileName).To(e => e.ProfileName))
            // .Join<UserNameChanged>(b => b
            //     .On(r => r.UserId)
            //     .Set(u => u.UserName).To(e => e.UserName))
            // .Join<ProfileNameChanged>(b => b
            //     .On(r => r.UserId)
            //     .Set(u => u.ProfileName).To(e => e.Name))
            .RemovedWith<UserRemoved>())
        .Children(g => g.ExerciseGroups, b => b
            .IdentifiedBy(eg => eg.ExerciseGroupId)
            .From<ExerciseGroupAddedToGroup>(b => b
                .UsingKey(e => e.ExerciseGroupId)
                .Set(eg => eg.Permission).To(e => e.Permission))

            // .Join<ExerciseGroupAdded>(b => b
            //     .On(r => r.ExerciseGroupId)
            //     .Set(eg => eg.Name).To(e => e.Name))
            .RemovedWith<ExerciseGroupRemovedFromGroup>())
        .Children(g => g.Roles, b => b
            .IdentifiedBy(r => r.RoleId)
            .From<RoleAddedToGroup>(b => b
                .UsingKey(e => e.RoleId))

            // .Join<RoleAdded>(b => b
            //     .On(r => r.RoleId)
            //     .Set(r => r.Name).To(e => e.Name))
            // .Join<RoleRenamed>(b => b
            //     .On(r => r.RoleId)
            //     .Set(r => r.Name).To(e => e.Name))
            .RemovedWith<RoleRemovedFromGroup>());
}
