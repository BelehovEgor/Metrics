using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metrics;

public static class LKMetricsCounter
{
    public static IEnumerable<ClassInfo> Get(
        ICollection<ClassDeclarationSyntax> classDeclarations, 
        ICollection<InterfaceDeclarationSyntax> interfaceDeclarations)
    {
        foreach (var classDeclaration in classDeclarations)
        {
            var name = classDeclaration.Identifier.ValueText;
            var parentClassName = GetParentClassName(classDeclaration);
            var classSize = CalculateClassSize(classDeclaration);
            var overriddenOperations = CalculateOverriddenOperations(classDeclaration);
            var addedOperations = CalculateAddedOperations(classDeclaration);
            var inheritanceDepth = CalculateInheritanceDepth(
                classDeclaration,
                classDeclarations,
                interfaceDeclarations);
            var specializationIndex = CalculateSpecializationIndex(
                inheritanceDepth,
                methodCount: addedOperations + overriddenOperations,
                overriddenOperations);
            var (operationComplexity, methodInfos) = CalculateOperationComplexity(classDeclaration);
            var averageNumberOfParametersPerOperation =
                CalculateAverageNumberOfParametersPerOperation(methodInfos);

            yield return new ClassInfo(
                name,
                parentClassName,
                classSize,
                inheritanceDepth,
                overriddenOperations,
                addedOperations,
                specializationIndex,
                operationComplexity,
                methodInfos,
                averageNumberOfParametersPerOperation);
        }
    }
    
    private static string? GetParentClassName(ClassDeclarationSyntax classDeclaration)
    {
        var baseTypeSyntax = classDeclaration.BaseList?.Types.FirstOrDefault();

        return baseTypeSyntax?.Type is SimpleNameSyntax simpleNameSyntax 
            ? simpleNameSyntax.Identifier.ValueText 
            : null;
    }

    static int CalculateClassSize(ClassDeclarationSyntax node)
    {
        var methods = node.DescendantNodes().OfType<MethodDeclarationSyntax>().Count();
        var properties = node.DescendantNodes().OfType<PropertyDeclarationSyntax>().Count();

        return methods + properties;
    }
    
    private static int CalculateOverriddenOperations(ClassDeclarationSyntax node)
    {
        var overriddenMethods = node.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Count(method => method.Modifiers.Any(modifier => modifier.ValueText == "override"));

        return overriddenMethods;
    }
    
    private static int CalculateAddedOperations(ClassDeclarationSyntax node)
    {
        var addedMethods = node.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Count(method => method.Modifiers.All(modifier => modifier.ValueText != "override"));
        
        return addedMethods;
    }
    
    private static int CalculateInheritanceDepth(
        ClassDeclarationSyntax classDeclaration,
        ICollection<ClassDeclarationSyntax> classDeclarations,
        ICollection<InterfaceDeclarationSyntax> interfaceDeclarations)
    {
        if (classDeclaration.BaseList is null)
        {
            return 0;
        }

        var maxDepth = 0;
        
        foreach (var baseTypeName in classDeclaration.BaseList.Types
                     .Where(x => x.Type is SimpleNameSyntax)
                     .Select(x => x.Type as SimpleNameSyntax))
        {
            var baseClassName = baseTypeName!.Identifier.ValueText;

            var baseClass = FindBaseClass(baseClassName, classDeclarations);
            var baseInterface = FindBaseInterface(baseClassName, interfaceDeclarations);

            if (baseClass != null)
            {
                var currDepth = CalculateInheritanceDepth(baseClass, classDeclarations, interfaceDeclarations);
                if (currDepth > maxDepth)
                {
                    maxDepth = currDepth;
                }
                break;
            }
            
            if (baseInterface != null)
            {
                var currDepth = 1;
                if (currDepth > maxDepth)
                {
                    maxDepth = currDepth;
                }
            }
        }

        return maxDepth;
    }
    
    private static ClassDeclarationSyntax? FindBaseClass(
        string baseClassName,
        IEnumerable<ClassDeclarationSyntax> classDeclarations)
    {
        return classDeclarations.FirstOrDefault(c => c.Identifier.ValueText.Equals(baseClassName));
    }
    
    private static InterfaceDeclarationSyntax? FindBaseInterface(
        string baseClassName,
        IEnumerable<InterfaceDeclarationSyntax> interfaceDeclarations)
    {
        return interfaceDeclarations.FirstOrDefault(c => c.Identifier.ValueText.Equals(baseClassName));
    }
    
    private static double CalculateSpecializationIndex(
        int depth, 
        int methodCount,
        int overriddenOperations)
    {
        if (methodCount == 0) return 0F;
        
        return (double)(overriddenOperations * depth) / methodCount;
    }
    
    private static (double, ICollection<MethodInfo>) CalculateOperationComplexity(
        ClassDeclarationSyntax classDeclaration,
        double methodCallCost = 5,
        double assignCost = 0.5,
        double mathOperationCost = 2,
        double messagesWithParametersCost = 3,
        double parameterCost = 0.3,
        double callCost = 7,
        double variableCost = 0.5,
        double messageWithoutParametersCost = 1)
    {
        var totalComplexity = 0.0;
        var methodsInfos = new List<MethodInfo>();

        foreach (var methodNode in classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var methodInfo = CalculateMethodComplexity(
                methodNode,
                methodCallCost,
                assignCost,
                mathOperationCost,
                messagesWithParametersCost,
                parameterCost,
                callCost,
                variableCost,
                messageWithoutParametersCost);

            totalComplexity += methodInfo.Complexity;
            methodsInfos.Add(methodInfo);
        }

        return (totalComplexity, methodsInfos);
    }
    
     private static MethodInfo CalculateMethodComplexity(
         MethodDeclarationSyntax method,
         double methodCallCost = 5,
         double assignCost = 0.5,
         double mathOperationCost = 2,
         double messagesWithParametersCost = 3,
         double parameterCost = 0.3,
         double callCost = 7,
         double variableCost = 0.5,
         double messageWithoutParametersCost = 1)
    {
        var methodName = method.Identifier.ValueText;
        
        double complexity = 0.0;

        var parametersCount = method.DescendantNodes().OfType<ParameterSyntax>().Count();
        complexity += parametersCount * parameterCost;
        
        var variables = method.DescendantNodes().OfType<VariableDeclarationSyntax>();
        complexity += variables.Count() * variableCost;
        
        foreach (InvocationExpressionSyntax invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var argumentCount = invocation.ArgumentList.Arguments.Count;

            if (argumentCount > 0)
            {
                complexity += messagesWithParametersCost;
            }
            else if (invocation.Expression is IdentifierNameSyntax)
            {
                complexity += callCost;
            }
            else if (invocation.Expression is MemberAccessExpressionSyntax)
            {
                complexity += methodCallCost;
            }
            else
            {
                complexity += messageWithoutParametersCost;
            }
        }
        
        var assignments = method.DescendantNodes().OfType<AssignmentExpressionSyntax>();
        complexity += assignments.Count() * assignCost;
        
        var arithmeticOperations = method.DescendantNodes()
            .OfType<BinaryExpressionSyntax>()
            .Where(expr => 
                expr.Kind() == SyntaxKind.AddExpression || 
                expr.Kind() == SyntaxKind.SubtractExpression || 
                expr.Kind() == SyntaxKind.MultiplyExpression ||
                expr.Kind() == SyntaxKind.DivideExpression);
        complexity += arithmeticOperations.Count() * mathOperationCost;
        
        return new MethodInfo(methodName, parametersCount, complexity);
    }
    
    private static double CalculateAverageNumberOfParametersPerOperation(ICollection<MethodInfo> methodInfos)
    {
        if (!methodInfos.Any())
        {
            return 0;
        }
        
        return (double) methodInfos.Select(x => x.ParametersCount).Sum() / methodInfos.Count;
    }
}