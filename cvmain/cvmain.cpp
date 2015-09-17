#include <iostream>
#include <cstdio>
#include <cmath>
#include <map>
#include <time.h>

#include <opencv2/video/video.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/imgproc/imgproc.hpp>
#include <opencv2/core/core.hpp>
#include <opencv2/features2d/features2d.hpp>

#include "cvmain.h"
#include "videoInput.h"

#define M_PI 3.14159265359f
#define WIDTH 320
#define HEIGHT 240

using namespace std;
using namespace cv;

void tracking(Mat& frame, Mat& output);
bool isGoodPoint(int i);
float angleBetween(const Point2f& v1, const Point2f& v2);
float getInterval();

FRAME_CALLBACK OnData = NULL;
QUIT_CALLBACK OnQuit = NULL;

clock_t nowClk;

Mat gray, prevgray;
Mat rawframe, frame;
Mat result;

bool isTracking = false;
bool Quitted = true;

DLL_EXPORT void
CVSetFrameEvent(FRAME_CALLBACK callback)
{
	OnData = callback;
}

DLL_EXPORT void
CVSetQuitEvent(QUIT_CALLBACK callback)
{
	OnQuit = callback;
}

DLL_EXPORT bool
CVInit()
{
	// 打开 cap 会初始化相关 COM 组件，之后便可以调用 OpenCV 中隐藏的 
	// videoInput 库函数。只适用于 Windows
	try
	{
		VideoCapture cap(0);
	}
	catch (...)
	{
		return false;
	}

	return true;
}

DLL_EXPORT void
CVStart(char* stream)
{
	VideoCapture cap;
	isTracking = true;
	Quitted = false;

	int ret = 0;

	if (strchr(stream, '.') == NULL)
	{
		try
		{
			cap.open(atoi(stream));
		}
		catch (...)
		{
		}
	}
	else
	{
		cap.open(string(stream));
	}

	do
	{
		cap >> rawframe; // 有些摄像头第一帧会empty
	} while (rawframe.empty());

	while (isTracking)
	{
		tracking(rawframe, result);
		cap >> rawframe;

		if (rawframe.empty())
		{
			break;
		}
	}

	gray.release();
	prevgray.release();
	cap.release();
	OnQuit(false);
	Quitted = true;
}

DLL_EXPORT void
CVQuit()
{
	if (isTracking == false)
	{
		Quitted = true;
		isTracking = false;
	}
	else
	{
		Quitted = false;
		isTracking = false;
	}
}

DLL_EXPORT void
CVWaitForQuit()
{
	while (!Quitted)
	{
		Sleep(10);
	}
}

DLL_EXPORT int
CVGetCamCount()
{
	return videoInput::listDevices();
}

DLL_EXPORT char*
CVGetCamName(int id)
{
	return videoInput::getDeviceName(id);
}

DLL_EXPORT bool
CVTestCam(int id)
{
	VideoCapture cap;
	bool ret = false;

	if (isTracking)
		return false;

	try
	{
		cap.open(id);
		cap >> rawframe; // 有些摄像头第一帧会empty
		cap >> rawframe;
	}
	catch (...)
	{
		return false;
	}

	ret = !(rawframe.empty());

	cap.release();

	return ret;
}

struct HashCompare
{
public:
	bool operator()(const Point2f& lhs, const Point2f& rhs)
	{
		return lhs.y * WIDTH + lhs.x < rhs.y * WIDTH + rhs.x;
	}
};

map<Point2f, Point2f, HashCompare> motionDesc;

void
tracking(Mat& rawframe, Mat& output)
{
	bool update = false;
	Point2f movementVec(0, 0);
	float interval = getInterval();

	resize(rawframe, frame, Size(WIDTH, HEIGHT));
	cvtColor(frame, gray, CV_BGR2GRAY);
	equalizeHist(gray, gray);

	if (prevgray.empty())
	{
		gray.copyTo(prevgray);
	}

	// 简单的光流法检测运动
	if (motionDesc.size() < 30)
	{
		vector<KeyPoint> newFeatures;

		FAST(gray, newFeatures, 31, false);
		size_t nNewFeatures = (newFeatures.size() < 50) ? newFeatures.size() : 50;

		for (size_t i = 0; i < nNewFeatures; i++)
		{
			size_t idx = i * newFeatures.size() / nNewFeatures;
			motionDesc[newFeatures[idx].pt] = newFeatures[idx].pt;
		}
	}

	vector<Point2f> pointsAsKey, pointsToTrack, pointsTracked;

	for (map<Point2f, Point2f>::iterator it = motionDesc.begin(); it != motionDesc.end(); ++it)
	{
		pointsAsKey.push_back(it->first);
	}

	for (map<Point2f, Point2f>::iterator it = motionDesc.begin(); it != motionDesc.end(); ++it)
	{
		pointsToTrack.push_back(it->second);
	}

	if (motionDesc.size() > 0)
	{
		vector<uchar> status;
		vector<float> err;

		calcOpticalFlowPyrLK(prevgray, gray, pointsToTrack, pointsTracked, status, err);

		for (size_t i = 0; i < motionDesc.size(); i++)
		{
			if (status[i])
			{
				if (angleBetween(
					pointsTracked[i] - pointsToTrack[i], 
					pointsToTrack[i] - pointsAsKey[i]) 
					< 0.2)
				{
					// 移动方向不变，则有效位移越来越长
					motionDesc[pointsAsKey[i]] = pointsTracked[i];
					movementVec.x += pointsTracked[i].x - pointsAsKey[i].x;
					movementVec.y += pointsTracked[i].y - pointsAsKey[i].y;
					//line(frame, pointsAsKey[i], pointsTracked[i], Scalar(0, 0, 255));
					//circle(frame, pointsTracked[i], 2, Scalar(255, 0, 0), -1);
				}
				else
				{
					// 移动方向改变，则更新参考点
					motionDesc.erase(pointsAsKey[i]);
					motionDesc[pointsToTrack[i]] = pointsTracked[i];
				}
			}
			else
			{
				motionDesc.erase(pointsAsKey[i]);
			}
		}

		movementVec *= 1.0 / motionDesc.size();
	}

	cout << sqrt(movementVec.x * movementVec.x + movementVec.y * movementVec.y) << endl;

	if ((motionDesc.size() > 15)
		&& (sqrt(movementVec.x * movementVec.x + movementVec.y * movementVec.y) > 0.5))
	{
		update = true;
	}

	if (isTracking)
	{
		OnData(0 - movementVec.x, movementVec.y, interval, frame.data, update);
	}

	swap(prevgray, gray);
}

float
angleBetween(const Point2f& v1, const Point2f& v2)
{
	float len1 = sqrt(v1.x * v1.x + v1.y * v1.y);
	float len2 = sqrt(v2.x * v2.x + v2.y * v2.y);

	float dot = v1.x * v2.x + v1.y * v2.y;

	float a = dot / (len1 * len2);

	if (a >= 1.0)
		return 0.0;
	else if (a <= -1.0)
		return M_PI;
	else
		return acos(a);
}

float
getInterval()
{
	// 用 TickMeter 也可以
	clock_t tmp = nowClk;
	nowClk = clock();
	return (float)(nowClk - tmp) / CLOCKS_PER_SEC;
}

BOOL WINAPI
DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
	switch (fdwReason)
	{
	case DLL_PROCESS_ATTACH:
		// attach to process
		// return FALSE to fail DLL load
		break;

	case DLL_PROCESS_DETACH:
		// detach from process
		break;

	case DLL_THREAD_ATTACH:
		// attach to thread
		break;

	case DLL_THREAD_DETACH:
		// detach from thread
		break;
	}

	return TRUE; // succesful
}
