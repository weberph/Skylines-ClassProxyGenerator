using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ClassProxyGenerator
{
    class AssemblyResolver : IDisposable
    {
        private readonly string _basePath;
        private readonly Dictionary<string, Assembly> _loadedAssemblies = new Dictionary<string, Assembly>();

        public AssemblyResolver(string basePath)
        {
            _basePath = basePath;

            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        }

        public Assembly[] LoadedAssemblies
        {
            get { return _loadedAssemblies.Values.ToArray(); }
        }

        public Assembly Resolve(object sender, ResolveEventArgs args)
        {
            var name = args.Name.Substring(0, args.Name.IndexOf(','));

            Assembly assembly;
            if (_loadedAssemblies.TryGetValue(name, out assembly))
                return assembly;

            var assemblyFile = Path.Combine(_basePath, name + ".dll");
            if (File.Exists(assemblyFile))
            {
                assembly = Assembly.LoadFrom(assemblyFile);
                _loadedAssemblies.Add(name, assembly);
                return assembly;
            }

            return null;
        }

        public void Load(AssemblyName name)
        {
            Resolve(null, new ResolveEventArgs(name.FullName));
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= Resolve;
        }
    }
}
