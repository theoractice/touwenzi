#include <cstdio>
#include <cmath>
#include <iostream>
#include <map>
#include <time.h>

#include <opencv2/contrib/contrib.hpp>
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

TickMeter tm;

// ������ʾ����ͼ��ֱ��ȫ������
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
	// ��ʼ�� COM ������ɵ��� OpenCV ���ص� videoInput ������ȡ����ͷ��������Ϣ��֧�ֶ������ͷ��
	// �˷��������� Windows������ϵͳԭ�����ơ�
	return SUCCEEDED(CoInitialize(NULL));
}

DLL_EXPORT void
CVStart(char* stream)
{
	VideoCapture cap;
	Mat rawFrame;
	isTracking = true;

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
		// ����Ƶ�ļ�������Ҳ���Ե�
		cap.open(string(stream));
	}

	do
	{
		cap >> rawFrame; // ��Щ����ͷ��һ֡��empty
	} while (rawFrame.empty());

	Mat prevGray;

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
	// ��������ͷ���ޱ�ռ�û��ܷ���������
	Mat rawFrame;
	VideoCapture cap;
	bool ret = false;

	if (isTracking)
		return false;

	try
	{
		cap.open(id);
		cap >> rawFrame; // ��Щ����ͷ��һ֡��empty
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

	tm.stop();
	float interval = (float)tm.getTimeMilli();
	tm.reset();
	tm.start();

	resize(rawFrame, frame, Size(WIDTH, HEIGHT));
	cvtColor(frame, gray, CV_BGR2GRAY);
	equalizeHist(gray, gray);

	if (prevGray.empty())
	{
		gray.copyTo(prevGray);
	}

	// �򵥵Ĺ���������˶�
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

	vector<Point2f> ptsKey, ptsValToTrack, ptsTracked;

	for (map<Point2f, Point2f>::iterator i = motionDesc.begin(); i != motionDesc.end(); ++i)
	{
		ptsKey.push_back(i->first);
		ptsValToTrack.push_back(i->second);
	}

	Point2f movementVec(0, 0);

	if (motionDesc.size() > 0)
	{
		vector<uchar> status;
		vector<float> err;

		calcOpticalFlowPyrLK(prevGray, gray, ptsValToTrack, ptsTracked, status, err);

		for (size_t i = 0; i < motionDesc.size(); i++)
		{
			if (status[i])
			{
				if (abs(
					angleBetween(
					ptsTracked[i] - ptsValToTrack[i],
					ptsValToTrack[i] - ptsKey[i]))
					< 0.2)
				{
					// �ƶ����򲻱䣬����Чλ��Խ��Խ��
					motionDesc[ptsKey[i]] = ptsTracked[i];
					movementVec.x += ptsTracked[i].x - ptsKey[i].x;
					movementVec.y += ptsTracked[i].y - ptsKey[i].y;
					//line(frame, ptsKey[i], ptsTracked[i], Scalar(0, 0, 255));
					//circle(frame, ptsTracked[i], 2, Scalar(255, 0, 0), -1);
				}
				else
				{
					// �ƶ�����ı䣬����²ο���
					motionDesc.erase(ptsKey[i]);
					motionDesc[ptsValToTrack[i]] = ptsTracked[i];
				}
			}
			else
			{
				motionDesc.erase(ptsKey[i]);
			}
		}

		movementVec *= 1.0f / motionDesc.size();
	}

	bool update = false;

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
	{
		return 0.0;
	}
	else if (a <= -1.0)
	{
		return M_PI;
	}
	else
	{
		return acos(a);
	}
}

BOOL WINAPI
DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
	switch (fdwReason)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_PROCESS_DETACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	default:
		break;
	}

	return TRUE;
}
