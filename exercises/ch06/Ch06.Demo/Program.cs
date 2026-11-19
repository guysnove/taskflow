// Chapter 6 demo: run the requirement linter over sentences.
//   dotnet run -- interviews      the raw statements from the Brightside interviews
//   dotnet run -- ours            the rewritten requirements
using Ch06.Requirements;

string[] interviews =
[
    "I want to be notified about everything.",
    "Notifications should be instant.",
    "Teams should work like Slack.",
    "Nobody should be able to see client work they're not on.",
    "Remind me before things are due.",
];
string[] ours =
[
    "FR-NOTIF-1: When a task is assigned to a user, that user gets an in-app notification.",
    "FR-NOTIF-2: A user gets a notification when a task assigned to them is due within N days (default 2).",
    "NFR-NOTIF-1: An assignment notification appears in the app within 1 minute, 95% of the time.",
    "FR-TEAM-2: A team task is visible to, and editable by, every member of its team, and by no one else.",
];
string[] chosen = args.FirstOrDefault() == "ours" ? ours : interviews;
int flagged = 0;
foreach (string sentence in chosen)
{
    var findings = RequirementLint.Check(sentence);
    if (findings.Count > 0) flagged++;
    Console.WriteLine($"\"{sentence}\"");
    foreach (var (word, why) in findings) Console.WriteLine($"    {word,-10} {why}");
    if (findings.Count == 0) Console.WriteLine("    (nothing flagged)");
}
Console.WriteLine($"{flagged} of {chosen.Length} sentences flagged.");
