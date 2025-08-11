using Sandbox;
using System.Reflection;
using System.Runtime.Loader;

namespace defcon33;

public class Entrypoint
{
    //private static string PayloadPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\sbox\\sideload\\darklands.dlfortwars\\payload.dll";
    private static string PayloadPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\sbox\\sideload\\fish.blocks_and_bullets\\package.library.fish.payload.dll";

    public static int Main(string args)
    {
        System.IO.File.WriteAllText("context-inject.txt", "Loaded at " + DateTime.Now);
        
        Log.Info("ContextInject execution started.");

        //LoadDLLWithContext("dlfortwar", "package.darklands.dlfortwars", PayloadPath);

        LoadDLLWithContext("blocks_and_bullets", "package.fish.blocks_and_bullets", PayloadPath);

        return 0;
    }

    private static bool LoadDLLWithContext(string game_ident, string target_package, string payload_path)
    {
        if (Game.InGame && Game.Ident != null && Game.Ident.Contains(game_ident))
        {
            var pkgEntry = AssemblyLoadContext.All
                .SelectMany(alc => alc.Assemblies, (alc, asm) => new { alc, asm })
                .FirstOrDefault(x => x.asm.GetName().Name == target_package);

            if (pkgEntry == null)
            {
                Log.Info($"{target_package} not found in any ALC.");
                return false;
            }

            var pkgAlc = pkgEntry.alc;
            var pkgAsm = pkgEntry.asm;

            Log.Info($"Found package in ALC: {pkgAlc}, Type={pkgAlc.GetType().FullName}");


            using var fs = File.OpenRead(payload_path);
            var payloadAsm = pkgAlc.LoadFromStream(fs);
            Log.Info($"Loaded payload assembly: {payloadAsm.FullName}");
            ExecuteClassMethod(payloadAsm, "payload.Addon", "OnLoad");

            return true;
        }

        return false;
    }

    public static void ExecuteClassMethod(Assembly assembly, string className, string methodName)
    {
        string? assemblyName = assembly.GetName().Name;
        System.Type? type = assembly.GetType(className);
        if (type == null)
        {
            Log.Error($"Class {className} does not exist in assembly {assemblyName}!");
            return;
        }

        // Create a new instance of the class.
        object? classInstance = null;
        try
        {
            classInstance = System.Activator.CreateInstance(type, null);
        }
        catch (System.MissingMethodException) {/* This is fine, its a static class! */}

        // Get the entry point method.
        MethodInfo? methodInfo = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (methodInfo == null)
        {
            // Because sometimes you DONT need to have a method, it is optional (OnUpdate).
            Log.Warning($"Failed to find method {assemblyName}.{className}.{methodName}.");
            return;
        }

        // Checks params then call the entry point method.
        ParameterInfo[] parameters = methodInfo.GetParameters();
        if (parameters.Length == 0)
        {
            try
            {
                methodInfo.Invoke(classInstance, null);
            }
            catch (Exception ex)
            {
                Log.Info($"Hit an error during invoke: {ex}");
            }
        }
        else
        {
            Log.Error($"{assemblyName}.{className}.{methodName} cannot have any parameters!");
        }
    }
}
