```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

public interface IAccountNotifications
{
    Task Notify(string accountId);
}

public interface IAccountAuditLog
{
    void Record(string accountId, EventSequenceNumber sequenceNumber);
}

public class AccountAuditor(IAccountNotifications notifications) : IReadModelReactor
{
    public Task Modified(Account account, EventContext context, IAccountAuditLog audit)
    {
        audit.Record(account.Id, context.SequenceNumber);
        return notifications.Notify(account.Id);
    }
}
```
