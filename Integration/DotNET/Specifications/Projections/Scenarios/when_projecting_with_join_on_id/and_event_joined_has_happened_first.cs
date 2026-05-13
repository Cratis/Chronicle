// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;
using context = Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_with_join_on_id.and_event_joined_has_happened_first.context;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_with_join_on_id;

[Collection(ChronicleCollection.Name)]
public class and_event_joined_has_happened_first(context context) : Given<context>(context)
{
    const string BootTitle = "MyBook";
    const string UserName = "User";

    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<BorrowedBooksProjection, BorrowedBook>(chronicleFixture)
    {
        public Guid UserId;
        public Guid BookId;
        public override IEnumerable<Type> EventTypes => [typeof(BookAddedToInventory), typeof(UserOnboarded), typeof(BookBorrowed)];

        void Establish()
        {
            UserId = Guid.Parse("3c760aaf-2119-4336-8721-3f4c97e86a1b");
            EventSourceId = UserId.ToString();
            BookId = Guid.Parse("462ec4f6-fd9e-4549-92b9-00b769636468");

            EventsWithEventSourceIdToAppend.Add(new(BookId, new BookAddedToInventory(BootTitle, string.Empty, string.Empty)));
            EventsWithEventSourceIdToAppend.Add(new(UserId.ToString(), new UserOnboarded(UserName, string.Empty)));
            EventsWithEventSourceIdToAppend.Add(new(BookId, new BookBorrowed(UserId)));
        }

        protected override Task<BorrowedBook> GetReadModelResult()
        {
            var result = ChronicleFixture.ReadModels.Database.GetCollection<BorrowedBook>().Find(_ => _.Id == BookId);
            return result.FirstOrDefaultAsync();
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_user_name() => Context.Result.User.ShouldEqual(UserName);
    [Fact] void should_have_book_id() => Context.Result.Id.ShouldEqual(Context.BookId);
    [Fact] void should_have_book_title() => Context.Result.Title.ShouldEqual(BootTitle);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
