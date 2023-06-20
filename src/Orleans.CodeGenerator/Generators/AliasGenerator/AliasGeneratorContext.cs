namespace Orleans.CodeGenerator.Generators.AliasGenerator;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal class AliasGeneratorContext : IncrementalGeneratorContext
{
    public List<(TypeSyntax Type, string Alias)> TypeAliases { get; } = new(1024);


}
