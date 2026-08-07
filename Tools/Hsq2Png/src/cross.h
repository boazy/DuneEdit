#pragma once

#include <codecvt>
#include <string>
#include <vector>

#ifdef TARGET_OS_MAC
#	define MAC
#endif

#ifdef __linux__
#	define LINUX
#endif

#if defined(_WIN32) || defined(_WIN64)
#	define WINDOWS
#endif

#ifdef WINDOWS
#	include "targetver.h"
#	include <windows.h>
#endif

#if defined(WINDOWS) && defined(_UNICODE)
#	define WRAP_UTF8_MAIN()																\
		int wmain(int argc, wchar_t* wargv[])											\
		{																				\
			std::vector<char*> argv(argc);												\
			std::vector<std::string> args;												\
			args.reserve(argc);															\
																						\
			for (int i = 0; i < argc; i++)												\
			{																			\
				std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>, wchar_t> cvt;	\
				args.push_back(cvt.to_bytes(wargv[i]));									\
				argv[i] = const_cast<char*>(args[i].c_str());							\
			}																			\
																						\
			return utf8_main(argc, argv.data());										\
		}
#else
#   define WRAP_UTF8_MAIN()						\
		int main(int argc, char* wargv[])		\
		{										\
			return utf8_main(argc, argv);		\
		}
#endif
