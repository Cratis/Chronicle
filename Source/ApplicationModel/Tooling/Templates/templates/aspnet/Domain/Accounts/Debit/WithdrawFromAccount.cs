using Concepts;
using Concepts.Accounts;
using Events.Accounts.Debit;

namespace Domain.Accounts.Debit
{
    public record WithdrawFromAccount(AccountId AccountId, double Amount);
}