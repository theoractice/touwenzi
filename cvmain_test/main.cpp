#include <iostream>
#include <opencv2/video/video.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/imgproc/imgproc.hpp>
#include <opencv2/core/core.hpp>
#include <opencv2/features2d/features2d.hpp>

#include "../cvmain/cvmain.h"
using namespace cv;
using namespace std;

int counts = 0;

void OnFrame(float x, float y, float interval, void* image, bool update)
{
    cout<<update<<endl;
    Mat tmpMat(240, 320, CV_8UC3, image, 960);
    imshow("Test", tmpMat);
    int key = waitKey(1);

//    if(counts++>20)
//        CVQuit();
}

void OnQuit(bool camState)
{
    cout<<"444"<<endl;
}

int main(int argc, char* argv[])
{
    FRAME_CALLBACK cbFrame = OnFrame;
    QUIT_CALLBACK cbQuit = OnQuit;
    CVSetFrameEvent(cbFrame);
    CVSetQuitEvent(cbQuit);
    CVInit();
    CVStart("test.avi");
}


