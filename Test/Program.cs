using NLang;
using System.Diagnostics;

string path = "./Programs/test.nl";

var result = Runner.RunFile(path);
Console.WriteLine(result.result);
Console.WriteLine("Time: " + result.time + "ms");


Console.ReadLine();