using PQLang;
using System.Diagnostics;

string path = "./Programs/test3.pq";

var result = Runner.RunFile(path);
Console.WriteLine(result.result);
Console.WriteLine("Time: " + result.time + "ms");


Console.ReadLine();