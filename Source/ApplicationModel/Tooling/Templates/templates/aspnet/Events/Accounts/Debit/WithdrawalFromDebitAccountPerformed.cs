namespace Events.Accounts.Debit
{
    [EventType("507a71d9-862f-4615-b8e8-2427d9568373")]
    public record WithdrawalFromDebitAccountPerformed(double Amount);
}
