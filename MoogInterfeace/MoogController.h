#pragma once


#include "MBC_Interface.h"
#include "ScriptFile.h"

#define DllExport   __declspec( dllexport )

extern "C" class DllExport MoogController
{
private:

	CConfigFile* m_config;
	ofstream* m_logFile;
	CMBCInterface* m_mbcInterface;
	CScriptFile* m_scriptFile;

public:
	MoogController(char initFilePath[], ofstream* logFile);

	void Connect();

	void Engage();

	void Disengage();

	void SendPositions(MoogFrame* position);

	void SendPosition(double surge, double heave, double lateral, double yaw, double roll, double pitch);

	void Disconnect();
};

char x[260] = "C:\\Moog\\MotionBaseHost.ini";
//char cFilePath[MAX_PATH + 1];
//strcpy_s(cFilePath, MAX_PATH, "C:\\Moog\\MotionBaseHost.ini");
ofstream logFile("C:\\Users\\user\\Desktop\\logFile.txt");
MoogController* moogController = new MoogController(x, &logFile);

extern "C" DllExport void Connect()
{
	moogController->Connect();
}

extern "C" DllExport void Engage()
{
	moogController->Engage();
}

extern "C" DllExport void Disengage()
{
	moogController->Disengage();
}

extern "C" DllExport void SendPositions(MoogFrame* position)
{
	moogController->SendPositions(position);
}

extern "C" DllExport void SendPosition(double surge, double heave, double lateral, double yaw, double roll, double pitch)
{
	moogController->SendPosition(surge, heave, lateral, yaw, roll, pitch);
}

extern "C" DllExport void Disconnect()
{
	moogController->Disconnect();
}