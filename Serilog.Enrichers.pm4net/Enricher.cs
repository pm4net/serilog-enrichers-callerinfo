using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Enrichers.pm4net
{
    public class Enricher : ILogEventEnricher
    {
        private readonly bool _includeCallerInfo;
        private readonly bool _includeFileInfo;
        private readonly IEnumerable<string> _allowedAssemblies;
        private readonly string _prefix;

        public Enricher(bool includeCallerInfo, bool includeFileInfo, IEnumerable<string> allowedAssemblies, string prefix = "pm4net_")
        {
            _includeCallerInfo = includeCallerInfo;
            _includeFileInfo = includeFileInfo;
            _allowedAssemblies = allowedAssemblies ?? new List<string>();
            _prefix = prefix;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (_includeCallerInfo)
            {
                AddCallerInfo(logEvent);
            }
        }

        /// <summary>
        /// Add information about the origin of the logged message, such as method, namespace, and file information (from debugging symbols).
        /// </summary>
        /// <param name="logEvent">The logged event.</param>
        private void AddCallerInfo(LogEvent logEvent)
        {
            var sb = new StringBuilder();
            var st = EnhancedStackTrace.Current();

            var frame = st.FirstOrDefault(x => x.HasMethod() && x.MethodInfo.IsInAllowedAssembly(_allowedAssemblies));
            var method = frame?.MethodInfo.MethodBase;
            var type = method?.DeclaringType;

            if (type != null)
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}Method", new ScalarValue(method.Name)));
                logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}Namespace", new ScalarValue(type.FullName)));

                if (_includeFileInfo)
                {
                    var fileName = frame.GetFileName();
                    if (fileName != null)
                    {
                        logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}SourceFile", new ScalarValue(fileName)));
                        logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}LineNumber", new ScalarValue(frame.GetFileLineNumber())));
                        logEvent.AddPropertyIfAbsent(new LogEventProperty($"{_prefix}ColumnNumber", new ScalarValue(frame.GetFileColumnNumber())));
                    }
                }
            }
        }
    }

    internal static class Extensions
    {
        /// <summary>
        /// Determines whether the resolved method originates in one of the allowed assemblies.
        /// </summary>
        /// <param name="method">The method to look up.</param>
        /// <param name="allowedAssemblies">A list of fully qualified assembly names to check against.</param>
        /// <returns>True if the method originates from one of the allowed assemblies, false otherwise.</returns>
        internal static bool IsInAllowedAssembly(this ResolvedMethod method, IEnumerable<string> allowedAssemblies)
        {
            var type = method.DeclaringType;
            if (type != null)
            {
                var assemblyName = type.Assembly.GetName().Name;
                return allowedAssemblies.Contains(assemblyName, StringComparer.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
