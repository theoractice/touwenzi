#ifndef __CVMAIN_H__
#define __CVMAIN_H__

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

/*  To use this exported function of dll, include this header
 *  in your project.
 */

#ifdef CVMAIN_EXPORTS
#define DLL_EXPORT __declspec(dllexport)
#else
#define DLL_EXPORT __declspec(dllimport)
#endif

#ifdef __cplusplus
extern "C"
{
#endif

	typedef void(CALLBACK* FRAME_CALLBACK)(float x, float y, float interval, void* pImage, bool update);
	typedef void(CALLBACK* QUIT_CALLBACK)(bool isCamError);

	DLL_EXPORT void CVSetFrameEvent(FRAME_CALLBACK callback);
	DLL_EXPORT void CVSetQuitEvent(QUIT_CALLBACK callback);
	DLL_EXPORT bool CVInit();
	DLL_EXPORT void CVStart(char* FILE);
	DLL_EXPORT void CVQuit();
	DLL_EXPORT void CVWaitForQuit();
	DLL_EXPORT int CVGetCamCount();
	DLL_EXPORT char* CVGetCamName(int id);
	DLL_EXPORT bool CVTestCam(int id);

#ifdef __cplusplus
}
#endif

#endif // __CVMAIN_H__
