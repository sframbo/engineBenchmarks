using BenchmarkDotNet.Running;
using ConsoleApp3;


Console.WriteLine(BenchmarkRunner.Run<BenchParsers>());
Console.WriteLine(BenchmarkRunner.Run<BenchRenderers>());