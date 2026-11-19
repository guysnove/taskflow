using System.Text.RegularExpressions;

namespace Ch06.Requirements;

// A tiny "requirement linter": flags words that usually mean a requirement can't be tested yet.
// It can't understand a sentence. It only points a human at places worth a question.
public static class RequirementLint
{
    private static readonly (string Pattern, string Why)[] Rules =
    [
        (@"\b(fast|quick(ly)?|instant(ly)?|responsive|real[- ]time)\b", "how fast? give a number and a condition"),
        (@"\b(easy|easily|simple|intuitive|user[- ]friendly)\b", "for whom? how would we check it?"),
        (@"\b(all|every(thing)?|always|never|any)\b", "really all? list the cases, or the exceptions"),
        (@"\b(etc\.?|and so on|such as)\b", "an open list: what exactly is included?"),
        (@"\b(should|could|might|may)\b", "a wish or a rule? use MoSCoW for priority instead"),
        (@"\b(and/or)\b", "which one? both?"),
        (@"\b(like|similar to)\b", "like it in which way?"),
        (@"\b(soon|before|recent(ly)?|later)\b", "how long, exactly?"),
        (@"\b(appropriate|reasonable|adequate|as needed|efficient(ly)?)\b", "who decides? write the criterion"),
    ];

    public static List<(string Word, string Why)> Check(string sentence)
    {
        var found = new List<(string, string)>();
        foreach (var (pattern, why) in Rules)
            foreach (Match m in Regex.Matches(sentence, pattern, RegexOptions.IgnoreCase))
                found.Add((m.Value, why));
        return found;
    }
}
