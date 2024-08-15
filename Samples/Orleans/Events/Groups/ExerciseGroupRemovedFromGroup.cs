using Concepts.ExerciseGroups;

namespace Events.Groups;

[EventType]
public record ExerciseGroupRemovedFromGroup(ExerciseGroupId ExerciseGroupId);
