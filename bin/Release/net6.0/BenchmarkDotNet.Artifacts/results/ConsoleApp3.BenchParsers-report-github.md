``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1889 (21H1/May2021Update)
AMD Ryzen 5 3600, 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.301
  [Host]     : .NET 6.0.6 (6.0.622.26707), X64 RyuJIT
  DefaultJob : .NET 6.0.6 (6.0.622.26707), X64 RyuJIT


```
|           Method |         Mean |      Error |     StdDev |   Gen 0 |  Gen 1 | Allocated |
|----------------- |-------------:|-----------:|-----------:|--------:|-------:|----------:|
|          Scriban |    10.626 μs |  0.1771 μs |  0.1739 μs |  0.8087 | 0.0305 |      7 KB |
| &#39;Scriban Liquid&#39; |    11.181 μs |  0.1841 μs |  0.1632 μs |  0.9155 | 0.0305 |      8 KB |
|        DotLiquid |    20.747 μs |  0.3219 μs |  0.2688 μs |  2.1057 | 0.0305 |     17 KB |
|          Stubble |    11.396 μs |  0.1057 μs |  0.0988 μs |  1.3123 | 0.0153 |     11 KB |
|         Nustache |    49.902 μs |  0.3584 μs |  0.2993 μs |  2.3193 |      - |     20 KB |
|   Handlebars.NET | 3,103.079 μs | 42.9313 μs | 38.0575 μs | 19.5313 | 7.8125 |    162 KB |
|           Cottle |    11.251 μs |  0.1895 μs |  0.1773 μs |  1.7395 | 0.0458 |     14 KB |
|            Fluid |     8.440 μs |  0.1178 μs |  0.1044 μs |  0.3204 |      - |      3 KB |
