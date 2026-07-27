// InteropInspector, dump types/methods/fields from managed DLLs via System.Reflection.Metadata.
// Usage: InteropInspector <dir-of-dlls> <type-fullname-regex>
// Output: assembly | type | method|field | member name | (params/type detail)

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;

namespace InteropInspector;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: InteropInspector <dir-of-dlls> <type-fullname-regex>");
            Console.Error.WriteLine(@"Example: InteropInspector D:\OperatorHSR\gamefiles\MelonLoader\Il2CppAssemblies ""LoadoutManager|WeaponV2|GunStats""");
            return 2;
        }

        string dir = args[0];
        if (!Directory.Exists(dir))
        {
            Console.Error.WriteLine($"Directory not found: {dir}");
            return 3;
        }

        Regex rx;
        try
        {
            rx = new Regex(args[1], RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Bad regex: {ex.Message}");
            return 2;
        }

        using var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false), 1 << 16) { AutoFlush = false };

        int filesScanned = 0, filesSkipped = 0, typesMatched = 0;

        foreach (string file in Directory.EnumerateFiles(dir, "*.dll"))
        {
            try
            {
                typesMatched += InspectFile(file, rx, stdout);
                filesScanned++;
            }
            catch (Exception ex) // native DLLs, corrupt images, locked files, etc.
            {
                filesSkipped++;
                Console.Error.WriteLine($"# skipped {Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
            }

            stdout.Flush(); // stream results out per-assembly
        }

        stdout.Flush();
        Console.Error.WriteLine($"# done: {filesScanned} assemblies scanned, {filesSkipped} skipped, {typesMatched} matching types");
        return 0;
    }

    private static int InspectFile(string path, Regex rx, TextWriter @out)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1 << 16, FileOptions.SequentialScan);
        using var pe = new PEReader(fs);

        if (!pe.HasMetadata)
            return 0; // native / resource-only DLL

        MetadataReader md = pe.GetMetadataReader();
        string asm = Path.GetFileNameWithoutExtension(path);
        var provider = TypeNameProvider.Instance;
        int matched = 0;

        foreach (TypeDefinitionHandle tdh in md.TypeDefinitions)
        {
            TypeDefinition td = md.GetTypeDefinition(tdh);
            string fullName = FullTypeName(md, td);
            if (!rx.IsMatch(fullName))
                continue;

            matched++;

            foreach (MethodDefinitionHandle mh in td.GetMethods())
            {
                MethodDefinition m = md.GetMethodDefinition(mh);
                string name = md.GetString(m.Name);
                string detail;
                try
                {
                    // Parameter row names, keyed by sequence number (0 = return value).
                    var paramNames = new Dictionary<int, string>();
                    foreach (ParameterHandle ph in m.GetParameters())
                    {
                        Parameter p = md.GetParameter(ph);
                        if (p.SequenceNumber > 0 && !p.Name.IsNil)
                            paramNames[p.SequenceNumber] = md.GetString(p.Name);
                    }

                    MethodSignature<string> sig = m.DecodeSignature(provider, genericContext: null);
                    var parts = new string[sig.ParameterTypes.Length];
                    for (int i = 0; i < sig.ParameterTypes.Length; i++)
                    {
                        string pn = paramNames.TryGetValue(i + 1, out string n) ? n : $"arg{i}";
                        parts[i] = $"{sig.ParameterTypes[i]} {pn}";
                    }
                    detail = $"({string.Join(", ", parts)}) -> {sig.ReturnType}";
                }
                catch (Exception)
                {
                    detail = "(signature decode failed)";
                }

                @out.WriteLine($"{asm} | {fullName} | method | {name} | {detail}");
            }

            foreach (FieldDefinitionHandle fh in td.GetFields())
            {
                FieldDefinition f = md.GetFieldDefinition(fh);
                string fieldType;
                try { fieldType = f.DecodeSignature(provider, genericContext: null); }
                catch (Exception) { fieldType = "?"; }

                @out.WriteLine($"{asm} | {fullName} | field | {md.GetString(f.Name)} | ({fieldType})");
            }
        }

        return matched;
    }

    private static string FullTypeName(MetadataReader md, TypeDefinition td)
    {
        string name = md.GetString(td.Name);
        TypeDefinitionHandle declaring = td.GetDeclaringType();
        if (!declaring.IsNil)
            return FullTypeName(md, md.GetTypeDefinition(declaring)) + "/" + name;

        string ns = td.Namespace.IsNil ? "" : md.GetString(td.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }
}

/// <summary>Renders type handles in signatures as readable names.</summary>
internal sealed class TypeNameProvider : ISignatureTypeProvider<string, object>
{
    public static readonly TypeNameProvider Instance = new();

    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
    {
        PrimitiveTypeCode.Void => "void",
        PrimitiveTypeCode.Boolean => "bool",
        PrimitiveTypeCode.Char => "char",
        PrimitiveTypeCode.SByte => "sbyte",
        PrimitiveTypeCode.Byte => "byte",
        PrimitiveTypeCode.Int16 => "short",
        PrimitiveTypeCode.UInt16 => "ushort",
        PrimitiveTypeCode.Int32 => "int",
        PrimitiveTypeCode.UInt32 => "uint",
        PrimitiveTypeCode.Int64 => "long",
        PrimitiveTypeCode.UInt64 => "ulong",
        PrimitiveTypeCode.Single => "float",
        PrimitiveTypeCode.Double => "double",
        PrimitiveTypeCode.String => "string",
        PrimitiveTypeCode.Object => "object",
        PrimitiveTypeCode.IntPtr => "nint",
        PrimitiveTypeCode.UIntPtr => "nuint",
        PrimitiveTypeCode.TypedReference => "typedref",
        _ => typeCode.ToString().ToLowerInvariant(),
    };

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        TypeDefinition td = reader.GetTypeDefinition(handle);
        string ns = td.Namespace.IsNil ? "" : reader.GetString(td.Namespace);
        string name = reader.GetString(td.Name);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        TypeReference tr = reader.GetTypeReference(handle);
        string ns = tr.Namespace.IsNil ? "" : reader.GetString(tr.Namespace);
        string name = reader.GetString(tr.Name);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    public string GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public string GetSZArrayType(string elementType) => elementType + "[]";

    public string GetArrayType(string elementType, ArrayShape shape)
        => elementType + "[" + new string(',', Math.Max(0, shape.Rank - 1)) + "]";

    public string GetByReferenceType(string elementType) => "ref " + elementType;

    public string GetPointerType(string elementType) => elementType + "*";

    public string GetFunctionPointerType(MethodSignature<string> signature)
        => $"fnptr({string.Join(", ", signature.ParameterTypes)}) -> {signature.ReturnType}";

    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
    {
        // Strip the arity suffix (List`1 -> List) for readability.
        int tick = genericType.LastIndexOf('`');
        string baseName = tick >= 0 ? genericType[..tick] : genericType;
        return $"{baseName}<{string.Join(", ", typeArguments)}>";
    }

    public string GetGenericMethodParameter(object genericContext, int index) => "!!" + index;

    public string GetGenericTypeParameter(object genericContext, int index) => "!" + index;

    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;

    public string GetPinnedType(string elementType) => elementType + " pinned";
}
