// Chapter 11 demo.
//   dotnet run -- a | b        Parnas's KWIC index, two designs, the same output
//   dotnet run -- leak         what happens when data isn't encapsulated
using Ch11.Kwic.Encapsulation;

string mode = args.FirstOrDefault() ?? "b";
if (mode == "leak")
{
    var leaky = new LeakyProject();
    leaky.Tasks.Add("Write copy"); leaky.OpenCount++;
    leaky.Tasks.Add("Get logo");                            // someone forgot OpenCount++
    leaky.Done.Add("Write copy"); leaky.OpenCount--;
    Console.WriteLine($"Leaky:        OpenCount = {leaky.OpenCount}, really open = {leaky.Tasks.Except(leaky.Done).Count()}");

    var project = new Project();
    project.Add("Write copy"); project.Add("Get logo"); project.Complete("Write copy");
    Console.WriteLine($"Encapsulated: OpenCount = {project.OpenCount}");
    try { project.Complete("Unknown"); }
    catch (InvalidOperationException ex) { Console.WriteLine($"Encapsulated: refused: {ex.Message}"); }
    return;
}

string[] titles =
[
    "Software Engineering Economics",
    "The Mythical Man Month",
    "On the Criteria To Be Used in Decomposing Systems into Modules",
];
var lines = mode == "a" ? Ch11.Kwic.SharedData.KwicA.Run(titles) : Ch11.Kwic.Hiding.KwicB.Run(titles);
foreach (var l in lines) Console.WriteLine(l);
Console.WriteLine($"({lines.Count} lines, design {(mode == "a" ? "A: shared data" : "B: information hiding")})");
