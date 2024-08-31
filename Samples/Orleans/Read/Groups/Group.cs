using Concepts.Groups;

namespace Read.Groups;

public class Group
{
    public DateTimeOffset LastUpdated { get; set; }
    public GroupId Id { get; set; } = GroupId.NotSet;
    public GroupName Name { get; set; } = GroupName.NotSet;
    public IList<GroupUser> Users { get; set; } = [];
    public IList<GroupExerciseGroup> ExerciseGroups { get; set; } = [];
    public IList<GroupRole> Roles { get; set; } = [];
    public bool IsSystem { get; set; }
}
