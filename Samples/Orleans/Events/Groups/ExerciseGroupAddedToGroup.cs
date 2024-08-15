using Concepts.ExerciseGroups;
using Concepts.Groups;

namespace Events.Groups;

[EventType]
public record ExerciseGroupAddedToGroup(ExerciseGroupId ExerciseGroupId, ExerciseGroupPermission Permission);
