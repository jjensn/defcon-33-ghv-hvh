#pragma once
#include "mscoree.h"
typedef HRESULT(STDAPICALLTYPE* FnGetCLRRuntimeHost)(REFIID riid, IUnknown** pUnk);