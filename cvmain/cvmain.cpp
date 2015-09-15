#include <iostream>
#include <cstdio>
#include <cmath>
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

int maxCount = 300;
double qLevel = 0.01;
double minDist = 5.0;
clock_t nowClk;

Mat gray, prevgray;
Mat rawframe, frame;
Mat result;

vector<Point2f> rawPoints[2];
vector<Point2f> basePoints;
vector<Point2f> fastFeatures;
vector<KeyPoint> featurePoints;
vector<uchar> status;		
vector<float> err;

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

	basePoints.clear();
	fastFeatures.clear();
	rawPoints[0].clear();
	rawPoints[1].clear();
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
	videoInput VI;
	VI.setVerbose(true);
	return VI.listDevices();
}

DLL_EXPORT char*
CVGetCamName(int id)
{
	videoInput VI;
	return VI.getDeviceName(id);
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

void
tracking(Mat& rawframe, Mat& output)
{
	bool update = true;
	Point2f movementVec(0, 0);
	float interval = getInterval();

	resize(rawframe, frame, Size(WIDTH, HEIGHT));
	cvtColor(frame, gray, CV_BGR2GRAY);
	equalizeHist(gray, gray);

	// 筛选特征点
	if (rawPoints[0].size() <= 30)
	{
		FAST(gray, featurePoints, 31, false);
		size_t num = (featurePoints.size() < 50) ? featurePoints.size() : 50;
		fastFeatures.clear();

		for (size_t i = 0; i < num; i++)
		{
			int idx = i * featurePoints.size() / num;
			fastFeatures.push_back(featurePoints[idx].pt);
		}

		rawPoints[0].insert(rawPoints[0].end(), fastFeatures.begin(), fastFeatures.end());
		basePoints.insert(basePoints.end(), fastFeatures.begin(), fastFeatures.end());
	}

	if (rawPoints[0].empty())
	{
		update = false;
		goto pp;
	}

	if (prevgray.empty())
	{
		gray.copyTo(prevgray);
	}

	calcOpticalFlowPyrLK(prevgray, gray, rawPoints[0], rawPoints[1], status, err);

	int k = 0;

	basePoints.resize(rawPoints[1].size());

	for (size_t i = 0; i < rawPoints[1].size(); i++)
	{
		if (isGoodPoint(i))
		{
			movementVec += rawPoints[1][i] - basePoints[i];
			basePoints[k] = basePoints[i];
			rawPoints[0][k] = rawPoints[0][i];
			rawPoints[1][k] = rawPoints[1][i];
			k++;
		}
	}

	//cout << k << endl;

	if (abs(movementVec.x) + abs(movementVec.y) < (k << 1))
	{
		update = false;
		goto pp;
	}

	basePoints.resize(k);
	rawPoints[0].resize(k);
	rawPoints[1].resize(k);

	if (k < 15)
	{
		update = false;
		goto pp;
	}

	for (size_t i = 0; i < rawPoints[1].size(); i++)
	{
		if (angleBetween(rawPoints[1][i] - rawPoints[0][i], rawPoints[1][i] - basePoints[i]) > 0.2)
			basePoints[i] = rawPoints[1][i];

		movementVec += rawPoints[1][i] - basePoints[i];
	}

	if (rawPoints[1].size() > 0)
		movementVec *= 1.0 / rawPoints[1].size();

	movementVec *= 1.0 / k;

pp:
	if (isTracking)
	{
		OnData(0 - movementVec.x, movementVec.y, interval, frame.data, update);
	}

	swap(rawPoints[1], rawPoints[0]);
	swap(prevgray, gray);
}

bool
isGoodPoint(const int i)
{
	return status[i] && ((abs(rawPoints[0][i].x - rawPoints[1][i].x) + abs(rawPoints[0][i].y - rawPoints[1][i].y)) > 2);
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
