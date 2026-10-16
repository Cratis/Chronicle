```csharp
using Cratis.Chronicle.AspNetCore.Transactions;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class SubscribeEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapPost("/courses/{courseId:guid}/students/{studentId:guid}", Handle);

    static async Task<IResult> Handle(
        Guid courseId,
        Guid studentId,
        HttpContext http,
        IDecisionReads reads,
        IEventLog eventLog)
    {
        // Get enrolls each read in the request's unit of work.
        var course = await reads.Get<CourseSeats>(courseId);
        var student = await reads.Get<StudentCourses>(studentId);

        if (course.Instance is not { } seats)
        {
            return Results.NotFound();
        }

        if (seats.Subscribed >= seats.Capacity || (student.Instance?.Subscribed ?? 0) >= 5)
        {
            return Results.UnprocessableEntity();
        }

        // Transactional.Append stages the event in the unit of work. A plain eventLog.Append
        // would be written immediately and would not be guarded by the decision reads.
        await eventLog.Transactional.Append(courseId, new CourseSeatTaken(studentId));
        await eventLog.Transactional.Append(studentId, new StudentJoinedCourse(courseId));

        // The middleware commits after the response is written, which is too late to report a
        // conflict. Commit here, while the response can still change.
        var unitOfWork = await http.Features.Get<IUnitOfWorkCompletionFeature>()!.CommitAsync();
        if (unitOfWork.GetDecisionConflicts().Any())
        {
            return Results.Conflict();
        }

        return unitOfWork.IsSuccess ? Results.NoContent() : Results.UnprocessableEntity();
    }
}
```
