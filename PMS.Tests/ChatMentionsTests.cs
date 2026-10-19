using PMS.Application.Common;

namespace PMS.Tests;

public class ChatMentionsTests
{
    private static readonly Dictionary<int, string> Room = new()
    {
        [5] = "Ann",
        [7] = "Ann Lee",
        [9] = "Bob (QA)",   // brackets/parentheses are not allowed inside a token
    };

    [Fact]
    public void A_token_for_a_room_member_is_kept_and_reported()
    {
        var (body, ids) = ChatMentions.Normalize("hi @[Ann Lee](7) and @[Ann](5)", Room);
        Assert.Equal("hi @[Ann Lee](7) and @[Ann](5)", body);
        Assert.Equal(new[] { 7, 5 }, ids);
    }

    [Fact]
    public void The_name_in_a_token_is_replaced_by_the_members_real_name()
    {
        var (body, ids) = ChatMentions.Normalize("@[Totally Someone Else](7)", Room);
        Assert.Equal("@[Ann Lee](7)", body);                 // cannot make "Ann Lee" look like another person
        Assert.Equal(new[] { 7 }, ids);
    }

    [Fact]
    public void A_token_for_someone_not_in_the_room_is_demoted_to_plain_text()
    {
        var (body, ids) = ChatMentions.Normalize("hello @[Stranger](99)!", Room);
        Assert.Equal("hello @Stranger!", body);
        Assert.Empty(ids);
    }

    [Fact]
    public void A_bare_id_pattern_without_a_real_token_mentions_nobody()
    {
        var (body, ids) = ChatMentions.Normalize("sneaky ](5) and [x](7) and @(7)", Room);
        Assert.Equal("sneaky ](5) and [x](7) and @(7)", body);
        Assert.Empty(ids);
    }

    [Fact]
    public void The_same_person_mentioned_twice_is_reported_once()
    {
        var (_, ids) = ChatMentions.Normalize("@[Ann](5) ... @[Ann](5)", Room);
        Assert.Equal(new[] { 5 }, ids);
    }

    [Fact]
    public void Brackets_are_stripped_from_names_so_the_token_stays_well_formed()
    {
        var (body, ids) = ChatMentions.Normalize("@[x](9)", Room);
        Assert.Equal("@[Bob QA](9)", body);
        Assert.Equal(new[] { 9 }, ids);
        Assert.Equal("Bob QA", ChatMentions.SafeName("Bob (QA)"));
    }

    [Fact]
    public void An_id_that_overflows_is_treated_as_not_a_member()
    {
        var (body, ids) = ChatMentions.Normalize("@[Ann](99999999999999999999)", Room);
        Assert.Equal("@Ann", body);
        Assert.Empty(ids);
    }

    [Fact]
    public void Text_without_tokens_is_untouched()
    {
        var (body, ids) = ChatMentions.Normalize("just words, an email a@b.com and @someone", Room);
        Assert.Equal("just words, an email a@b.com and @someone", body);
        Assert.Empty(ids);
    }
}
