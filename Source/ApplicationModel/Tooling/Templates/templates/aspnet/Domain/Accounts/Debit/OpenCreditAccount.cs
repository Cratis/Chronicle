// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Concepts.Accounts;

namespace Domain.Accounts.Debit;

public record OpenCreditAccount(
    [Required] AccountId AccountId,
    AccountDetails Details);
