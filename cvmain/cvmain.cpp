#include <cstdio>
#include <cmath>
#include <iostream>
#include <map>
#include <time.h>

#include <opencv2/core/core.hpp>
#include <opencv2/features2d/features2d.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/imgproc/imgproc.hpp>
#include <opencv2/video/video.hpp>

#include "cvmain.h"
#include "videoInput.h"

#define M_PI 3.14159265359f
#define WIDTH 320
#define HEIGHT 240

using namespace std;
using namespace cv;

void tracking(Mat& rawFrame, Mat& prevGray);
float angleBetween(const Point2f& v1, const Point2f& v2);
float getInterval();

FRAME_CALLBACK	OnData = NULL;
QUIT_CALLBACK	OnQuit = NULL;

clock_t nowClk;

// 窗体显示所需图像，直接全局声明
Mat frame;

bool isTracking = false;
HANDLE quitted = CreateEvent(NULL, FALSE, FALSE, L"quitted");

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

DLL_EXPORT BOOL
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
	Mat rawFrame, gray, prevGray;
	isTracking = true;

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
		cap >> rawFrame; // 有些摄像头第一帧会empty
	} while (rawFrame.empty());

	while (isTracking)
	{
		tracking(rawFrame, prevGray);
		cap >> rawFrame;

		if (rawFrame.empty())
		{
			isTracking = false;
		}
	}

	OnQuit(false);
	SetEvent(quitted);
}

DLL_EXPORT void
CVQuit()
{
	if (isTracking)
	{
		isTracking = false;
		WaitForSingleObject(quitted, INFINITE);
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

DLL_EXPORT BOOL
CVTestCam(int id)
{
	Mat rawFrame;
	VideoCapture cap;
	bool ret = false;

	if (isTracking)
		return false;

	try
	{
		cap.open(id);
		cap >> rawFrame; // 有些摄像头第一帧会empty
		cap >> rawFrame;
	}
	catch (...)
	{
		return false;
	}

	ret = !(rawFrame.empty());
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
tracking(Mat& rawFrame, Mat& prevGray)
{
	Mat gray;
	bool update = false;
	Point2f movementVec(0, 0);
	float interval = getInterval();

	resize(rawFrame, frame, Size(WIDTH, HEIGHT));
	cvtColor(frame, gray, CV_BGR2GRAY);
	equalizeHist(gray, gray);

	if (prevGray.empty())
	{
		gray.copyTo(prevGray);
	}

	// 简单的光流法检测运动
	if (motionDesc.size() < 50)
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

		calcOpticalFlowPyrLK(prevGray, gray, pointsToTrack, pointsTracked, status, err);

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

	if ((motionDesc.size() > 15)
		&& (sqrt(movementVec.x * movementVec.x + movementVec.y * movementVec.y) > 1.0f))
	{
		update = true;
	}

	if (isTracking)
	{
		//cout << sqrt(movementVec.x * movementVec.x + movementVec.y * movementVec.y) << endl;
		OnData(0 - movementVec.x, movementVec.y, interval, frame.data, update);
	}

	swap(prevGray, gray);
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
