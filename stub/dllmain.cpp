// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#pragma comment (lib, "user32.lib")

#define BUILD_PATH "C:\\Users\\hails\\Desktop\\DEFCON-TOOLS\\src\\defcon-33-gg\\x64"

bool bin_copy(const std::string& sourcePath, const std::string& destPath)
{
    std::ifstream input(sourcePath, std::ios::binary);
    if (!input) {
        std::cerr << "Failed to open source: " << sourcePath << std::endl;
        return false;
    }

    std::ofstream output(destPath, std::ios::binary);
    if (!output) {
        std::cerr << "Failed to open destination: " << destPath << std::endl;
        return false;
    }

    output << input.rdbuf();  

    std::cout << sourcePath << " -> " << destPath << std::endl;
    return true;
}

BOOL WINAPI inject_managed_library(std::filesystem::path loader)
{
    HMODULE CoreCLRModule;
    CoreCLRModule = GetModuleHandleA("coreclr.dll");
    if (CoreCLRModule == NULL) {
        std::cerr << "Did not find coreclr.dll" << std::endl;
        return FALSE;
    }
    ICLRRuntimeHost* RuntimeHost;
    FnGetCLRRuntimeHost pfnGetCLRRuntimeHost = (FnGetCLRRuntimeHost)::GetProcAddress(CoreCLRModule, "GetCLRRuntimeHost");

    if (!pfnGetCLRRuntimeHost)
    {
        std::cerr << "Did not find GetCLRRuntimeHost in EAT" << std::endl;
        return FALSE;
    }

    HRESULT hr = pfnGetCLRRuntimeHost(IID_ICLRRuntimeHost, (IUnknown**)&RuntimeHost);
    if (FAILED(hr))
    {
        std::cerr << "Failed calling GetCLRRuntimeHost" << std::endl;
        return FALSE;
    }

    hr = RuntimeHost->Start();
    if (FAILED(hr))
    {
        std::cerr << "Failed starting RuntimeHost" << std::endl;
        return FALSE;
    }

    DWORD ExitCode = -1;

    hr = RuntimeHost->ExecuteInDefaultAppDomain(loader.generic_wstring().c_str(), L"defcon33.Entrypoint", L"Main", L"", &ExitCode);
    
    if (FAILED(hr))
    {
        std::cerr << "Failed to execute - hr: " << hr << " " << std::endl;
        return FALSE;
    }
    
    return TRUE;
}

#ifdef _DEBUG
void open_console() 
{
    AllocConsole();

    // Redirect stdout
    FILE* outStream;
    freopen_s(&outStream, "CONOUT$", "w", stdout);

    // Redirect stderr
    FILE* errStream;
    freopen_s(&errStream, "CONOUT$", "w", stderr);

    SetConsoleTitleA("stub debug console");
}
#endif

DWORD WINAPI attach_thread(LPVOID module) 
{
#ifdef _DEBUG
    open_console();
#endif
    std::filesystem::path tpa_path("./bin/managed/");
    std::filesystem::create_directories(tpa_path);

    std::filesystem::path dest_path(tpa_path / "contextloader.dll");
    std::filesystem::path build_path(std::string(BUILD_PATH));

#ifdef _DEBUG
    std::filesystem::path src_path(build_path / "Debug\\net9.0\\contextloader.dll");
#else
    std::filesystem::path src_path(build_path / "Release\\net9.0\\contextloader.dll");
#endif

    // Copy the managed hotswap DLL to game path so its not done manually each build
    bin_copy(src_path.string(), dest_path.string());

    // Fire the cannons
    inject_managed_library(dest_path);

    std::cout << "All done!" << std::endl;

    return 0;
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);
        CreateThread(NULL, 0, attach_thread, hModule, 0, NULL);
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

