using System.Text.Json;
using Metrics;

var (classes, interfaces) = SolutionReader.GetClassesAndInterfaces("D:\\itmo\\verify\\CsharpSSA");

var classesInfo = LKMetricsCounter.Get(classes, interfaces).ToList();

await using StreamWriter outputFile = new StreamWriter("CsharpSSA_L&KMetrics.txt");

await outputFile.WriteAsync(JsonSerializer.Serialize(classesInfo));