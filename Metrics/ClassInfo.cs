namespace Metrics;

public record ClassInfo(
    string Name,
    string? ParentName,
    int ClassSize,
    int InheritanceDepth,
    int OverriddenOperations,
    int AddedOperations,
    double SpecializationIndex,
    double OperationComplexity,
    IEnumerable<MethodInfo> MethodInfos,
    double AverageNumberOfParametersPerOperation);
    
public record MethodInfo(
    string Name,
    int ParametersCount,
    double Complexity);