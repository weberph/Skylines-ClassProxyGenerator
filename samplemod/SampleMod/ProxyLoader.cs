using System;
using System.IO;
using System.Reflection;

namespace SampleMod
{
    class ProxyLoader
    {
        private readonly Type[] _exportedTypes;

        public ProxyLoader(string proxyAssemblyPath)
        {
            Mod.Log("Loading proxy assembly...");
            var raw = File.ReadAllBytes(proxyAssemblyPath);
            var proxyAssembly = Assembly.Load(raw);
            _exportedTypes = proxyAssembly.GetExportedTypes();
            Mod.Log("Proxy loaded");
        }

        public Type GetProxy<T>()
        {
            var name = typeof (T).Name + "Proxy";

            foreach (var exportedType in _exportedTypes)
            {
                if (exportedType.Name == name)
                {
                    return exportedType;
                }
            }

            Mod.Log("Proxy not found: " + name);
            return null;
        }
    }
}