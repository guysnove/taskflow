// Run in Release, with nothing else busy:   dotnet run -c Release -- --filter "*"
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
