```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

[EventType]
public record CourseDefined(string Name, int Capacity);

[EventType]
public record CourseCapacityChanged(int Capacity);

// Events of the course's own story.
[EventType]
public record CourseSeatTaken(Guid StudentId);

[EventType]
public record CourseSeatReleased(Guid StudentId);

// Events of the student's own story.
[EventType]
public record StudentJoinedCourse(Guid CourseId);

[EventType]
public record StudentLeftCourse(Guid CourseId);

[FromEvent<CourseDefined>]
public record CourseSeats(
    Guid Id,
    [SetFrom<CourseDefined>(nameof(CourseDefined.Capacity))]
    [SetFrom<CourseCapacityChanged>(nameof(CourseCapacityChanged.Capacity))]
    int Capacity,
    [Increment<CourseSeatTaken>]
    [Decrement<CourseSeatReleased>]
    int Subscribed);

[FromEvent<StudentJoinedCourse>]
public record StudentCourses(
    Guid Id,
    [Increment<StudentJoinedCourse>]
    [Decrement<StudentLeftCourse>]
    int Subscribed);

public enum SubscribeOutcome
{
    Subscribed,
    UnknownCourse,
    CourseIsFull,
    StudentHasTooManyCourses,
    ChangedConcurrently,
    Failed
}

public static class SubscribeStudentToCourse
{
    const int MaxCoursesPerStudent = 5;

    public static async Task<SubscribeOutcome> Handle(IEventStore eventStore, Guid courseId, Guid studentId)
    {
        var reads = eventStore.GetDecisionReads();
        var course = await reads.GetDetached<CourseSeats>(courseId);
        var student = await reads.GetDetached<StudentCourses>(studentId);

        if (course.Instance is not { } seats)
        {
            return SubscribeOutcome.UnknownCourse;
        }

        if (seats.Subscribed >= seats.Capacity)
        {
            return SubscribeOutcome.CourseIsFull;
        }

        if ((student.Instance?.Subscribed ?? 0) >= MaxCoursesPerStudent)
        {
            return SubscribeOutcome.StudentHasTooManyCourses;
        }

        // One fact, two event sources, two event types: each read model counts only the types of its own source.
        var result = await eventStore.EventLog.AppendMany(
            [
                new EventForEventSourceId(courseId, new CourseSeatTaken(studentId)),
                new EventForEventSourceId(studentId, new StudentJoinedCourse(courseId))
            ],
            guardedBy: [course, student]);

        if (result.GetDecisionConflicts([course, student]).Any())
        {
            // Either side changed after it was read. Nothing was appended for either source.
            return SubscribeOutcome.ChangedConcurrently;
        }

        return result.IsSuccess ? SubscribeOutcome.Subscribed : SubscribeOutcome.Failed;
    }
}
```
