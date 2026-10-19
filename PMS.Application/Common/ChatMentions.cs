using System.Text.RegularExpressions;

namespace PMS.Application.Common;

/// <summary>
/// A mention is written into a message as @[Display Name](userId). The server never trusts the
/// client's tokens: each one is kept only if that user is really in the room (and its name is
/// replaced by the member's current name); anything else is demoted to plain "@Name" text.
/// </summary>
public static partial class ChatMentions
{
    [GeneratedRegex(@"@\[([^\]]+)\]\((\d+)\)")]
    private static partial Regex Token();

    /// <summary>Brackets would break the token format, so they never appear in a name.</summary>
    public static string SafeName(string name) =>
        new string(name.Where(c => c is not ('[' or ']' or '(' or ')')).ToArray()).Trim();

    public static (string Body, IReadOnlyList<int> MentionedIds) Normalize(
        string body, IReadOnlyDictionary<int, string> roomMembers)
    {
        var ids = new List<int>();
        var normalized = Token().Replace(body, m =>
        {
            if (int.TryParse(m.Groups[2].Value, out var id) && roomMembers.TryGetValue(id, out var name))
            {
                if (!ids.Contains(id)) ids.Add(id);
                return $"@[{SafeName(name)}]({id})";
            }
            return $"@{m.Groups[1].Value}";
        });
        return (normalized, ids);
    }
}
