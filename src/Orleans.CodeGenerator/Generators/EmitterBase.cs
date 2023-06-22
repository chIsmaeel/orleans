namespace Orleans.CodeGenerator.Generators;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class UniqueId
{
    public static int Id { get; set; } = 0;
    public static HashSet<string> FileNames = new HashSet<string>();
}

internal abstract class EmitterBase
{
    protected readonly SourceProductionContext _sourceProductionContext;


    public EmitterBase(SourceProductionContext context)
    {
        _sourceProductionContext = context;
    }

    public abstract void Emit();

    protected static string ConvertCompilationUnitSyntaxIntoString(CompilationUnitSyntax compilationUnitSyntax)
    {
        return compilationUnitSyntax.NormalizeWhitespace().ToFullString();

    }

    public void AddSource(string fileName, string content)
    {
        //if (!UniqueId.FileNames.Add(fileName))
        //{
        //    UniqueId.Id++;
        //    fileName = $"{fileName}_{UniqueId.Id}";

        //}
        _sourceProductionContext.AddSource(fileName + ".g.cs", content);

    }

    protected static void AddMember(Dictionary<string, List<MemberDeclarationSyntax>> nsMembers, string ns, MemberDeclarationSyntax member)
    {
        if (!nsMembers.TryGetValue(ns, out var existing))
        {
            existing = nsMembers[ns] = new List<MemberDeclarationSyntax>();
        }

        existing.Add(member);
    }
}
