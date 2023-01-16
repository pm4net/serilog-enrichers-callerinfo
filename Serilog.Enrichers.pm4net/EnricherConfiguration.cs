using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Serilog.Configuration;

namespace Serilog.Enrichers.pm4net
{
    public static class EnricherConfiguration
    {
        public static LoggerConfiguration WithPm4Net(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            bool includeCallerInfo,
            bool includeFileInfo,
            IEnumerable<string> allowedAssemblies,
            string prefix = "pm4net_")
        {
            return enrichmentConfiguration.With(new Enricher(includeCallerInfo, includeFileInfo, allowedAssemblies, prefix));
        }

        public static LoggerConfiguration WithPm4Net(
            this LoggerEnrichmentConfiguration enrichmentConfiguration, 
            bool includeCallerInfo,
            bool includeFileInfo,
            string assemblyPrefix,
            string prefix = "pm4net_")
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            var referencedAssemblies = GetAssemblies(callingAssembly, asm => asm.Name.StartsWith(assemblyPrefix, StringComparison.OrdinalIgnoreCase));
            return enrichmentConfiguration.WithPm4Net(includeCallerInfo, includeFileInfo, referencedAssemblies, prefix);
        }

        /// <summary>
        /// Find the assemblies that a starting Assembly references, filtering with some predicate.<br/>
        /// Adapted from <see href="https://stackoverflow.com/a/10253634/2102106"/>
        /// </summary>
        /// <param name="start">The starting assembly.</param>
        /// <param name="filter">A filtering predicate based on the AssemblyName</param>
        /// <returns>The list of referenced Assembly names</returns>
        private static IEnumerable<string> GetAssemblies(Assembly start, Func<AssemblyName, bool> filter)
        {
            var asmNames = new List<string>();
            var stack = new Stack<Assembly>();

            stack.Push(start);

            do
            {
                var asm = stack.Pop();
                asmNames.Add(asm.GetName().Name);

                foreach (var reference in asm.GetReferencedAssemblies())
                {
                    if (!filter(reference))
                    {
                        continue;
                    }

                    if (!asmNames.Contains(reference.Name))
                    {
                        stack.Push(Assembly.Load(reference));
                        asmNames.Add(reference.Name);
                    }
                }
            } while (stack.Count > 0);

            return asmNames;
        }
    }
}
