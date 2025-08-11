//using HarmonyLib;
//using System;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Reflection.Metadata;
//using System.Runtime.Loader;

// this ended up not being used, but the hooks are kept around since you may be interested in looking at these methods for your own usage

//[HarmonyPatch]
//public static class AssemblyOrdererPatch
//{
//    static MethodBase TargetMethod()
//    {
//        var asm = AssemblyLoadContext.All
//        .SelectMany(a => a.Assemblies)
//        .FirstOrDefault(a => a.GetName().Name == "Sandbox.System");

//        if (asm == null)
//            return null; // not loaded yet

//        // No namespace — just the type name
//        var t = asm.GetType("AssemblyOrderer");
//        return t?.GetMethod("GetDependencyOrdered", BindingFlags.Public | BindingFlags.Instance);
//    }

//    static void Prefix(object __instance)
//    {
//        // Get private "entries" field
//        var entriesField = __instance.GetType().GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance);
//        var entriesList = entriesField.GetValue(__instance) as System.Collections.IList;

//        // Check if our assembly already exists in the list
//        bool exists = entriesList.Cast<object>()
//            .Any(e => (string)e.GetType().GetField("Ident", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
//                .GetValue(e) == "package.library.fish.payload.dll");

//        if (!exists)
//        {
//            // Create new Entry object
//            var entryType = entriesList[0].GetType(); // AssemblyOrderer.Entry
//            var newEntry = System.Activator.CreateInstance(entryType);

//            entryType.GetField("Ident", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
//                .SetValue(newEntry, "package.library.fish.payload.dll");

//            byte[] customBytes = File.ReadAllBytes("C:\\Program Files (x86)\\Steam\\steamapps\\common\\sbox\\sideload\\fish.blocks_and_bullets\\package.library.fish.payload.dll");
//            entryType.GetField("Bytes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
//                .SetValue(newEntry, customBytes);

//            // AssemblyName for dependency resolution
//            using (var ms = new MemoryStream(customBytes))
//            {
//                var peReader = new System.Reflection.PortableExecutable.PEReader(ms);
//                var metadataReader = peReader.GetMetadataReader();
//                string asmName = metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name);
//                entryType.GetField("AssemblyName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
//                    .SetValue(newEntry, asmName);

//                // No references
//                entryType.GetField("References", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
//                    .SetValue(newEntry, new System.Collections.Generic.List<string>());
//            }

//            Log.Info("Added my package?");
//            entriesList.Add(newEntry);
//        }
//    }
//}

//[HarmonyPatch]
//public static class BaseFileSystemPatch
//{
//    static MethodBase TargetMethod()
//    {
//        var asm = AssemblyLoadContext.All
//            .SelectMany(a => a.Assemblies)
//            .FirstOrDefault(a => a.GetName().Name == "Sandbox.Filesystem");

//        if (asm == null)
//            return null; // assembly not loaded yet

//        var t = asm.GetType("Sandbox.BaseFileSystem");
//        return t?.GetMethod("FileExists", BindingFlags.Public | BindingFlags.Instance);
//    }

//    // Prefix signature must match the original parameters, plus "ref bool __result" if you want to override
//    static bool Prefix(string path, ref bool __result)
//    {
//        if (path.EndsWith(".dll") )
//        {
//            Log.Info($"[Harmony] FileExists called: {path}");
//        }
        

//        // Example: force our custom file to always "exist"
//        if (path.EndsWith("payload.dll", StringComparison.OrdinalIgnoreCase))
//        {
//            Log.Info("[Harmony] Forcing FileExists = true");
//            __result = true;
//            return false; // skip original method
//        }

//        return true; // run original method
//    }
//}

//[HarmonyPatch]
//public static class VerifyAssemblyPatch
//{
//    static MethodBase TargetMethod()
//    {
//        var asm = AssemblyLoadContext.All
//            .SelectMany(a => a.Assemblies)
//            .FirstOrDefault(a => a.GetName().Name == "Sandbox.Access");

//        if (asm == null)
//            return null;

//        var t = asm.GetType("Sandbox.AccessControl");
//        return t?.GetMethod("VerifyAssembly", BindingFlags.Public | BindingFlags.Instance);
//    }

//    static bool Prefix(object __instance, Stream dll, out object outStream, bool addToWhitelist, ref bool __result)
//    {
//        // read all bytes from the incoming dll stream
//        using var ms = new MemoryStream();
//        dll.CopyTo(ms);
//        byte[] dllBytes = ms.ToArray();

//        // get TrustedBinaryStream type
//        var tbsType = __instance.GetType().Assembly.GetType("Sandbox.TrustedBinaryStream");

//        // call TrustedBinaryStream.CreateInternal(byte[])
//        var createInternal = tbsType.GetMethod("CreateInternal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
//        outStream = createInternal.Invoke(null, new object[] { dllBytes });

//        __result = true; // force pass
//        return false; // skip original
//    }
//}