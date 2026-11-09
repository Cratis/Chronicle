// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ChildrenSortValues.when_comparing;

public class strings_with_characters_above_the_basic_multilingual_plane : Specification
{
    const string Emoji = "\U0001F600";
    const string FullwidthA = "\uFF21";

    int _emojiToFullwidth;
    int _fullwidthToEmoji;
    int _emojiToItself;
    int _emojiToLongerEmojiString;

    void Because()
    {
        _emojiToFullwidth = ChildrenSortValues.Compare(Emoji, FullwidthA);
        _fullwidthToEmoji = ChildrenSortValues.Compare(FullwidthA, Emoji);
        _emojiToItself = ChildrenSortValues.Compare(Emoji, "\U0001F600");
        _emojiToLongerEmojiString = ChildrenSortValues.Compare(Emoji, $"{Emoji}a");
    }

    [Fact] void should_order_the_higher_code_point_after_even_though_its_utf16_code_unit_is_lower() => _emojiToFullwidth.ShouldBeGreaterThan(0);
    [Fact] void should_order_the_lower_code_point_before() => _fullwidthToEmoji.ShouldBeLessThan(0);
    [Fact] void should_consider_equal_strings_equal() => _emojiToItself.ShouldEqual(0);
    [Fact] void should_order_a_prefix_before_the_longer_string() => _emojiToLongerEmojiString.ShouldBeLessThan(0);
}
