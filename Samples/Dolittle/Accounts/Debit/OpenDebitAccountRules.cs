// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Events.Projections;
using FluentValidation;

namespace Sample.Accounts.Debit
{
    public abstract class StatefulBusinessRuleFor<T> : AbstractValidator<T>
    {
    }

    // public static class RuleExtensions
    // {
    //     public static IRuleBuilderOptions<T, TProperty> Unique<T, TProperty, TModel, TChildModel>(
    //         this IRuleBuilder<T, TProperty> ruleBuilder,
    //         Expression<Func<TModel, IEnumerable<TChildModel>>> childrenProperty
    //         )
    //     {
    //         throw new NotImplementedException();
    //     }
    // }

    public class OpenDebitAccountRules : StatefulBusinessRuleFor<OpenDebitAccount>
    {
        record AccountInfo(Guid Id, string Name);

        readonly IEnumerable<AccountInfo> _accountNames = Array.Empty<AccountInfo>();

        public OpenDebitAccountRules()
        {
            RuleFor(_ => _.Name); //.Unique(_ => _._accountNames);
            //.Unique(_ => _._accountNames, _ => _.Name)
            //.WithMessage("Account name needs to be unique");
        }

        public void DefineState(IProjectionBuilderFor<OpenDebitAccountRules> builder)
        {
            builder.Children(_ => _._accountNames, _ => _
                .IdentifiedBy(_ => _.Id)
                .From<DebitAccountOpened>(_ => _
                    .Set(m => m.Name).To(ev => ev.Name)));
        }
    }
}
