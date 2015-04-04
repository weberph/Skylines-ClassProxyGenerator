using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

namespace ClassProxyGenerator
{
    internal class ProxyGeneratorException : Exception
    {
        public ProxyGeneratorException(string message)
            : base(message)
        {
        }
    }

    internal class ProxyCompilationException : Exception
    {
        public EmitResult EmitResult { get; private set; }

        public ProxyCompilationException(EmitResult emitResult)
        {
            EmitResult = emitResult;
        }
    }

    class ProxyGenerator
    {
        private readonly Assembly _sourceAssembly;
        private readonly byte[] _rawSourceAssembly;
        private readonly string _csManagedDirectory;
        private readonly Assembly[] _references;
        private readonly Type[] _proxyTypes;

        public ProxyGenerator(byte[] rawSourceAssembly, string csManagedDirectory)
        {
            _rawSourceAssembly = rawSourceAssembly;
            _csManagedDirectory = csManagedDirectory;

            using (var assemblyResolver = new AssemblyResolver(csManagedDirectory))
            {
                _sourceAssembly = Assembly.Load(rawSourceAssembly);
                var modType = _sourceAssembly.ExportedTypes.FirstOrDefault(e => e.Name == "Mod");
                if (modType == null)
                {
                    throw new ProxyGeneratorException("No public class named 'Mod' found.");
                }

                var proxyTypesGetter = modType.GetProperty("ProxyTypes");
                if (proxyTypesGetter == null)
                {
                    throw new ProxyGeneratorException("No public static property 'ProxyTypes' of class 'Mod' found.");
                }

                _proxyTypes = (Type[])proxyTypesGetter.GetValue(null);

                var references = _sourceAssembly.GetReferencedAssemblies();
                foreach (var reference in references)
                {
                    if (reference.FullName.StartsWith("mscorlib"))
                        continue;   // Loading this mscorlib (2.0.0.0) results in mscorlib (4.0.0.0) being referenced.

                    assemblyResolver.Load(reference);
                }

                _references = assemblyResolver.LoadedAssemblies;
            }
        }

        public byte[] Compile()
        {
            var compilation = CreateCompilation();
            using (var stream = new MemoryStream())
            {
                var result = compilation.Emit(stream);
                if (!result.Success)
                {
                    throw new ProxyCompilationException(result);
                }
                return stream.ToArray();
            }
        }

        private string CreateProxyAssemblyName()
        {
            return "Proxy_" + DateTime.Now.ToString("hh-mm-ss");
        }

        private CSharpCompilation CreateCompilation()
        {
            var assemblyName = _sourceAssembly.GetName();
            var version = assemblyName.Version;
            var namespaceName = assemblyName.Name + "Proxy";

            var proxyClasses = _proxyTypes.Select(CreateProxyClass).Cast<MemberDeclarationSyntax>().ToArray();

            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(namespaceName))
                .AddMembers(proxyClasses);

            var attributes = SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Attribute(SyntaxFactory.ParseName("System.Reflection.AssemblyVersion"), SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(new[] {
                            SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression("\"" + version + "\""))
                        })))
                    })).WithTarget(SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.AssemblyKeyword)));

            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")))
                .AddMembers(namespaceDeclaration)
                .AddAttributeLists(attributes)
                .NormalizeWhitespace()
                ;

            var syntaxTree = CSharpSyntaxTree.Create(compilationUnit);

            var mscorlib = File.ReadAllBytes(Path.Combine(_csManagedDirectory, "mscorlib.dll"));
            var references = new[]
            {
                MetadataReference.CreateFromImage(mscorlib),
                MetadataReference.CreateFromImage(_rawSourceAssembly)
            }.Concat(_references.Select(MetadataReference.CreateFromAssembly));

            return CSharpCompilation.Create(CreateProxyAssemblyName(), new[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private static ClassDeclarationSyntax CreateProxyClass(Type type)
        {
            return SyntaxFactory.ClassDeclaration(type.Name + "Proxy")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(type.FullName)));
        }
    }
}
