using Concepts.ExerciseGroups;
using Concepts.Groups;

namespace Read.Groups;

public record GroupExerciseGroup(
    ExerciseGroupId ExerciseGroupId,
    ExerciseGroupName Name,
    ExerciseGroupPermission Permission);
