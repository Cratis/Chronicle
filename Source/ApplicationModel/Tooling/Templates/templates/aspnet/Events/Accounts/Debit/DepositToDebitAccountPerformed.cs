namespace Events.Accounts.Debit
{
    [EventType("adaab3e5-f797-4335-80d4-06758773f7e1")]
    public record DepositToDebitAccountPerformed(double Amount);
}
