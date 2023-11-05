using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metrics;

public static class SolutionReader
{
    public static (ICollection<ClassDeclarationSyntax>, ICollection<InterfaceDeclarationSyntax>) GetClassesAndInterfaces(
        string path)
    {
        var csharpFiles = Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories);

        var classes = new List<ClassDeclarationSyntax>();
        var interfaces = new List<InterfaceDeclarationSyntax>();
        foreach (var file in csharpFiles)
        {
            var fileContent = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(fileContent);

            var root = syntaxTree.GetRoot();
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            var interfaceDeclarations = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            
            classes.AddRange(classDeclarations);
            interfaces.AddRange(interfaceDeclarations);
        }

        return (classes, interfaces);
    }
}