// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AlertsOptions = Cratis.Chronicle.Configuration.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertConditions.when_checking_exclusion;

/// <summary>
/// A listed glob excludes every observer it matches: * for any number of characters and ? for exactly one.
/// </summary>
public class and_a_glob_is_listed : given.alert_conditions
{
    bool _matchedByStar;
    bool _matchedByQuestionMark;
    bool _notMatchedByQuestionMark;
    bool _notMatched;

    void Establish() => Configure(new AlertsOptions { ExcludeObservers = ["dev.*", "batch-?"] });

    void Because()
    {
        _matchedByStar = _conditions.IsExcluded("dev.orders.projection");
        _matchedByQuestionMark = _conditions.IsExcluded("batch-1");
        _notMatchedByQuestionMark = _conditions.IsExcluded("batch-12");
        _notMatched = _conditions.IsExcluded("orders");
    }

    [Fact] void should_exclude_what_star_matches() => _matchedByStar.ShouldBeTrue();
    [Fact] void should_exclude_what_question_mark_matches() => _matchedByQuestionMark.ShouldBeTrue();
    [Fact] void should_not_exclude_what_question_mark_does_not_match() => _notMatchedByQuestionMark.ShouldBeFalse();
    [Fact] void should_not_exclude_what_no_glob_matches() => _notMatched.ShouldBeFalse();
}
