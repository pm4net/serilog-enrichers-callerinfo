using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Serilog.Configuration;

namespace Serilog.Enrichers.CallerInfo
{
    public static class EnricherConfiguration
    {
        /// <summary>
        /// Enrich log events with information about the calling method.
        /// </summary>
        /// <param name="enrichmentConfiguration">The enrichment configuration.</param>
        /// <param name="includeFileInfo">Whether to include the caller's file information (file name, line number, column number).</param>
        /// <param name="allowedAssemblies">Which assemblies to consider when finding the calling method in the stack trace.</param>
        /// <param name="prefix">An optional prefix to prepend to all property values.</param>
        /// /// <param name="filePathDepth">The number of parent directories to include in the file name. If zero or negative, the full path is returned. If larger than the actual depth of the file, the full path is also returned.</param>
        /// <returns>The modified logger configuration.</returns>
        public static LoggerConfiguration WithCallerInfo(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            bool includeFileInfo,
            IEnumerable<string> allowedAssemblies,
            string prefix = "",
            int filePathDepth = 0)
        {
            return enrichmentConfiguration.With(new Enricher(includeFileInfo, allowedAssemblies, prefix, filePathDepth));
        }

        /// <summary>
        /// Enrich log events with information about the calling method. If using from appsettings.json, also provide the startingAssemblies as it will otherwise be a Serilog assembly that is inferred as the calling assembly.
        /// </summary>
        /// <param name="enrichmentConfiguration">The enrichment configuration.</param>
        /// <param name="includeFileInfo">Whether to include the caller's file information (file name, line number, column number).</param>
        /// <param name="assemblyPrefix">The prefix of assemblies to allow when finding the calling method in the stack trace.</param>
        /// <param name="prefix">An optional prefix to prepend to all property values.</param>
        /// <param name="startingAssembly">The optional name of the assembly from which to discover other related ones with the given prefix. If not provided, the calling assembly of this method is used as the starting point.</param>
        /// <param name="filePathDepth">The number of parent directories to include in the file name. If zero or negative, the full path is returned. If larger than the actual depth of the file, the full path is also returned.</param>
        /// <param name="excludedPrefixes">Which assembly prefixes to exclude when finding the calling method in the stack trace.</param>
        /// <returns>The modified logger configuration.</returns>
        public static LoggerConfiguration WithCallerInfo(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            bool includeFileInfo,
            string assemblyPrefix,
            string prefix = "",
            string startingAssembly = "",
            int filePathDepth = 0,
            IEnumerable<string> excludedPrefixes = null)
        {
            var startingAssemblies = new Stack<Assembly>();
            if (string.IsNullOrWhiteSpace(startingAssembly))
            {
                startingAssemblies.Push(Assembly.GetCallingAssembly());
                startingAssemblies.Push(Assembly.GetEntryAssembly());
            }
            else
            {
                startingAssemblies.Push(Assembly.Load(startingAssembly));
            }

            var referencedAssemblies = GetAssemblies(startingAssemblies,
                asm => asm.Name?.StartsWith(assemblyPrefix, StringComparison.OrdinalIgnoreCase) ?? false,
                asm => excludedPrefixes?.Any(excluded => asm?.Name?.StartsWith(excluded, StringComparison.OrdinalIgnoreCase) ?? false) ?? false);
            return enrichmentConfiguration.WithCallerInfo(includeFileInfo, referencedAssemblies, prefix, filePathDepth);
        }

        /// <summary>
        /// Find the assemblies that a starting Assembly references, filtering with some predicate.<br/>
        /// Adapted from <see href="https://stackoverflow.com/a/10253634/2102106"/>
        /// </summary>
        /// <param name="startingAssemblies">The starting assembly.</param>
        /// <param name="filter">A filtering predicate based on the AssemblyName</param>
        /// <param name="exclude">An exclusion predicate based on the AssemblyName</param>
        /// <returns>The list of referenced Assembly names</returns>
        private static IEnumerable<string> GetAssemblies(Stack<Assembly> startingAssemblies, Func<AssemblyName, bool> filter, Func<AssemblyName, bool> exclude=null)
        {
            var asmNames = new HashSet<string>(comparer: StringComparer.OrdinalIgnoreCase);

            do
            {
                var asm = startingAssemblies.Pop();
                if (!AssemblyExistsInList(asmNames, asm) && IsAssemblyIncluded(filter, asm) && !IsAssemblyExcluded(exclude, asm)  )
                {
                    asmNames.Add(asm.GetName().Name);
                }

                foreach (var reference in asm.GetReferencedAssemblies())
                {
                    var referenceAsm = Assembly.Load(reference);
                    if (AssemblyExistsInList(asmNames, referenceAsm) || !IsAssemblyIncluded(filter, referenceAsm) || IsAssemblyExcluded(exclude, referenceAsm))
                    {
                        continue;
                    }

                    startingAssemblies.Push(referenceAsm);
                    asmNames.Add(reference.Name);
                }

            } while (startingAssemblies.Count > 0);

            return asmNames;
        }

        private static bool IsAssemblyExcluded(Func<AssemblyName, bool> exclude, Assembly asm)
        {
            return exclude != null && exclude(asm.GetName());
        }

        private static bool IsAssemblyIncluded(Func<AssemblyName, bool> include, Assembly asm)
        {
            return include != null && include(asm.GetName());
        }

        private static bool AssemblyExistsInList(IEnumerable<string> asmNames, Assembly asm)
        {
            return asmNames.Contains(asm.GetName().Name);
        }
    }
}
