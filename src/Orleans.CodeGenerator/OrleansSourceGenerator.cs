using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Orleans.CodeGenerator.Diagnostics;
using Orleans.CodeGenerator.Generators;
using Orleans.CodeGenerator.Generators.AliasGenerators;
using Orleans.CodeGenerator.Generators.CompoundAliasGenerators;
using Orleans.CodeGenerator.Generators.MetadataGenerators;
using Orleans.CodeGenerator.Generators.SerializerGenerators;
using Orleans.CodeGenerator.Generators.WellKnownIdGenerators;

namespace Orleans.CodeGenerator
{
    [Generator]
    public sealed partial class OrleansSerializationSourceGenerator : IIncrementalGenerator
    {

        private static IncrementalValueProvider<TContext> GetIncrementalValueContext<TGenerator, TContext>(IncrementalGeneratorInitializationContext context) where TGenerator : BaseIncrementalGenerator, new() where TContext : IncrementalGeneratorContext
        {
            TGenerator generator = new();
            generator.Initialize(context);

            return generator.Execute(context).Select((ctx, _) => (TContext)ctx);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var processName = Process.GetCurrentProcess().ProcessName.ToLowerInvariant();
            if (processName.Contains("devenv") || processName.Contains("servicehub"))
            {
                return;
            }

            var aliasIncrementalValueContext = GetIncrementalValueContext<AliasGenerator, AliasGeneratorContext>(context);

            var compoundAliasIncrementalValueContext = GetIncrementalValueContext<CompoundAliasGenerator, CompoundAliasGeneratorContext>(context);

            var serializerIncrementalValueContext = GetIncrementalValueContext<Generators.SerializerGenerators.SerializerGenerator, SerializerGeneratorContext>(context);

            var metaDataIncrementalValueContext = GetIncrementalValueContext<Generators.MetadataGenerators.MetadataGenerator, MetadataGeneratorContext>(context);

            var wellKnownTypeIdIncrementalValueContext = GetIncrementalValueContext<WellKnownIdGenerator, WellKnownIdGeneratorContext>(context);


            var optionIncrementalValue = context.AnalyzerConfigOptionsProvider.Select((ctx, _) => Generators.SerializerGenerators.SerializerGenerator.GetCodeGeneratorOptions(ctx, _));


            var modelIncrementalValue = aliasIncrementalValueContext
                  .Combine(compoundAliasIncrementalValueContext)
                  .Combine(wellKnownTypeIdIncrementalValueContext)
                  .Combine(serializerIncrementalValueContext)
                  .Combine(metaDataIncrementalValueContext)
                  .Select((ctx, _) =>
                  {
                      var metadataModel = new MetadataModel()
                      {
                          TypeAliases = ctx.Left.Left.Left.Left.TypeAliases,
                          CompoundTypeAliases = ctx.Left.Left.Left.Right.CompoundTypeAliases,
                          WellKnownTypeIds = ctx.Left.Left.Right.WellKnownTypeIds,
                          SerializableTypes = ctx.Left.Right.SerializableTypes,
                          ActivatableTypes = ctx.Left.Right.ActivatableTypes,
                          ApplicationParts = ctx.Right.ApplicationParts,
                          DefaultCopiers = ctx.Left.Right.DefaultCopiers,
                          DetectedActivators = ctx.Left.Right.DetectedActivators,
                          DetectedConverters = ctx.Left.Right.DetectedConverters,
                          DetectedCopiers = ctx.Left.Right.DetectedCopiers,
                          DetectedSerializers = ctx.Left.Right.DetectedSerializers,
                          GeneratedInvokables = ctx.Left.Right.GeneratedInvokables,
                          GeneratedProxies = ctx.Left.Right.GeneratedProxies,
                          InvokableInterfaceImplementations = ctx.Left.Right.InvokableInterfaceImplementations,
                          InvokableInterfaces = ctx.Left.Right.InvokableInterfaces


                      };

                      return metadataModel;
                  }
              );


            var codeGeneratorIncrementalValue = context.CompilationProvider
                .Combine(optionIncrementalValue)

                .Select((tuple, _) => new CodeGenerator(tuple.Left, tuple.Right));
            context.RegisterSourceOutput(codeGeneratorIncrementalValue.Combine(modelIncrementalValue), RegisterSourceOutput);
        }

        private static void RegisterSourceOutput(SourceProductionContext context, (CodeGenerator CodeGenerator, MetadataModel MetadataModel) specs)
        {
            try
            {
                if (specs.CodeGenerator.Options == null)
                {
                    return;
                }

                if (specs.CodeGenerator.Options.IsLaunchDebugger)
                {
                    Debugger.Launch();
                }


                var compilationUnitSyntax = specs.CodeGenerator.GetCompilationUnitSyntax(specs.MetadataModel);

                var sourceString = compilationUnitSyntax.NormalizeWhitespace().ToFullString();
                context.AddSource($"{specs.CodeGenerator.Compilation.AssemblyName ?? "assembly"}.orleans", sourceString);

            }
            catch (Exception exception) when (HandleException(context, exception))
            {
            }
        }

        private static bool HandleException(SourceProductionContext context, Exception exception)
        {
            if (exception is OrleansGeneratorDiagnosticAnalysisException analysisException)
            {
                context.ReportDiagnostic(analysisException.Diagnostic);
                return true;
            }

            context.ReportDiagnostic(UnhandledCodeGenerationExceptionDiagnostic.CreateDiagnostic(exception));
            return false;
        }

    }
}