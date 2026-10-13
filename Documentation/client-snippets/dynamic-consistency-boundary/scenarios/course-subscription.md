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

[EventType]
public record StudentSubscribedToCourse(Guid StudentId, Guid CourseId);

[EventType]
public record StudentUnsubscribedFromCourse(Guid StudentId, Guid CourseId);

[FromEvent<CourseDefined>]
public record CourseSeats(
    Guid Id,
    [SetFrom<CourseDefined>(nameof(CourseDefined.Capacity))]
    [SetFrom<CourseCapacityChanged>(nameof(CourseCapacityChanged.Capacity))]
    int Capacity,
    [Increment<StudentSubscribedToCourse>]
    [Decrement<StudentUnsubscribedFromCourse>]
    int Subscribed);

[FromEvent<StudentSubscribedToCourse>]
public record StudentCourses(
    Guid Id,
    [Increment<StudentSubscribedToCourse>]
    [Decrement<StudentUnsubscribedFromCourse>]
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

        // One fact, two event sources: each read model folds only the events of its own source.
        var subscribed = new StudentSubscribedToCourse(studentId, courseId);
        var result = await eventStore.EventLog.AppendMany(
            [
                new EventForEventSourceId(courseId, subscribed),
                new EventForEventSourceId(studentId, subscribed)
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
