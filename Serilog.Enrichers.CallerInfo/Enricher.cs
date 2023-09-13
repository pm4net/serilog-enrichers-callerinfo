using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Enrichers.CallerInfo
{
    public class Enricher : ILogEventEnricher
    {
        private readonly bool _includeFileInfo;
        private readonly ImmutableHashSet<string> _allowedAssemblies;
        private readonly string _prefix;

        public Enricher(bool includeFileInfo, IEnumerable<string> allowedAssemblies, string prefix = "")
        {
            _includeFileInfo = includeFileInfo;
            _allowedAssemblies = allowedAssemblies.ToImmutableHashSet(equalityComparer: StringComparer.OrdinalIgnoreCase) ?? ImmutableHashSet<string>.Empty;
            _prefix = prefix ?? string.Empty;
        }

        /// <summary>
        /// Add information about the origin of the logged message, such as method, namespace, and file information (from debugging symbols).
        /// </summary>
        /// <param name="logEvent">The logged event.</param>
        /// <param name="propertyFactory">The property factory</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var st = EnhancedStackTrace.Current();

            var frame = st.FirstOrDefault(x => x.HasMethod() && x.MethodInfo.IsInAllowedAssembly(_allowedAssemblies));
            var method = frame?.MethodInfo.MethodBase;
            var type = method?.DeclaringType;

            if (!string.IsNullOrWhiteSpace(_prefix))
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty("Serilog.Enrichers.CallerInfo_Prefix", new ScalarValue(_prefix)));
            }

            if (type != null)
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}Method", new ScalarValue(method.Name)));
                logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}Namespace", new ScalarValue(type.FullName)));

                if (_includeFileInfo)
                {
                    var fullFileName = frame.GetFileName();
                    var fileName = GetCleanFileName(fullFileName);
                    if (fileName != null)
                    {
                        logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}SourceFile", new ScalarValue(fileName)));
                        logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}LineNumber", new ScalarValue(frame.GetFileLineNumber())));
                        logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}ColumnNumber", new ScalarValue(frame.GetFileColumnNumber())));
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets at most 3 levels of the full file name to make it easier to read and avoid leaking sensitive information.
        /// </summary>
        /// <param name="fullFileName"></param>
        /// <returns></returns>
        private static string GetCleanFileName(string fullFileName)
        {
            if (string.IsNullOrWhiteSpace(fullFileName))
            {
                return null;
            }
            var split = fullFileName.Split('\\');
            if (split.Length < 3)
            {
                return fullFileName;
            }
            return $"{split[split.Length - 3]}\\{split[split.Length - 2]}\\{split[split.Length - 1]}";
            
        }
    }

    internal static class Extensions
    {
        /// <summary>
        /// Determines whether the resolved method originates in one of the allowed assemblies.
        /// </summary>
        /// <param name="method">The method to look up.</param>
        /// <param name="allowedAssemblies">A HashSet of fully qualified assembly names to check against.</param>
        /// <returns>True if the method originates from one of the allowed assemblies, false otherwise.</returns>
        internal static bool IsInAllowedAssembly(this ResolvedMethod method, ImmutableHashSet<string> allowedAssemblies)
        {
            var type = method.DeclaringType;
            if (type != null)
            {
                var assemblyName = type.Assembly.GetName().Name;
                return allowedAssemblies.Contains(assemblyName);
            }

            return false;
        }
    }
}
