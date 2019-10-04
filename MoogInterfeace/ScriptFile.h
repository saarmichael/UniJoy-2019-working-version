// tabs=4
//************************************************************
//	COPYRIGHT 2010 Moog Inc. - ALL RIGHTS RESERVED
//
// This file is the product of Moog Inc. and cannot be 
// reproduced, copied, or used in any shape or form without 
// the express written consent of Moog Inc.
//************************************************************
//
//  Defines class that parses and executes a script file.
//  The script files drive the Motion-Base-Host interface
//  to send host commands to a Motion-Base-Computer.
//
//	Revision History: See end of file.
//
//*************************************************************
#include <string>
#include <iostream>
#include <fstream>

#include "ConfigFile.h"
#include "MBC_Interface.h"

#include <string.h>
#include "LogOutput.h"
#include "ConvertEnum.h"
#include <chrono>
#include <thread>

#include "MoogFrame.h"



#define MOTION_BASE_CENTER -0.22077500f

#define SURGE_MAX 0.25f				// Maximum Surge movement in meters.
#define LATERAL_MAX 0.25f			// Maximum Lateral movement in meters.
#define ROLL_MAX 0.50604949f			// Maximum Roll movement in radians.
#define PITCH_MAX 0.575850443f			// Maximum Pitch movement in radians.
#define YAW_MAX 0.50604949f				// Maximum Yaw movement in radians.

#define DOF_ROLL_COMMAND  "DOF roll "		//used for the command name of the DOF roll for the MBC.
#define DOF_PITCH_COMMAND  "DOF pitch "		//used for the command name of the DOF pitch (surge) for the MBC.
#define DOF_HEAVE_COMMAND  "DOF heave "		//used for the command name of the DOF heave for the MBC.
#define DOF_YAW_COMMAND "DOF yaw "			//used for the command name of the DOF yaw for the MBC.
#define DOF_LONG_COMMAND  "DOF long "		//used for the command name of the DOF long for the MBC.
#define DOF_LAT_COMMAND "DOF lat "			//used for the command name of the DOF lateral for the MBC.


using namespace std;

class CScriptFile
{
public:
	// Script commands
	typedef enum
	{
		COMMAND_INVALID = 0,
		COMMAND_MODE,
		COMMAND_ENGAGE,
		COMMAND_DISENGAGE,
		COMMAND_PAUSE,
		COMMAND_SIGNALGEN,
		COMMAND_WAITFORSTATE,
		COMMAND_LENGTH,
		COMMAND_RESET,
		COMMAND_DIRECT_DISPLACEMENT_DOF,
		COMMAND_DIRECT_DISPLACEMENT_LENGTH,
		COMMAND_BUFFET_DISPLACEMENT,
		COMMAND_BUFFET_ACCELERATION,
		COMMAND_WHITENOISE_DISPLACEMENT,
		COMMAND_WHITENOISE_ACCELERATION,
		COMMAND_DOF,
		COMMAND_MDA,
		COMMAND_SKIP,           // skip (n) commands to MBC (for testing of dropped packets)
		COMMAND_PLAYSTART
	} TECommand;

private:
	// script boundaries
	enum
	{

		//todo:check if this limit make some bugs.
		eMAX_NUM_COMMANDS = 20,                // max number of commands in a script
		eMAX_NUM_COMMAND_ARGUMENTS = 20,        // max number of arguments for a single command
		eMAX_NUM_COMMAND_ARGUMENT_LENGTH = 20   // max argument length
	};

	// maintains a single parsed command from the script file
	typedef struct
	{
		int         iLineNumber;    // script file line number
		TECommand   eCommand;       // command enumeration
		int         iNumArgs;       // number of arguments
		char        szArgs[eMAX_NUM_COMMAND_ARGUMENTS][eMAX_NUM_COMMAND_ARGUMENT_LENGTH + 1];
	} TCommand;

	int m_iCommandIndex;            // index of command being executed\

	int m_iNumCommands;             // number of commands in script
	TCommand m_stCommands[eMAX_NUM_COMMANDS];

	CMBCInterface* m_mbcInterface;

	ofstream* m_logFile;

public:
	CScriptFile(CMBCInterface* pMBC , ofstream* logFile);
	~CScriptFile();

	void SendMBCAxesPositions(MoogFrame* moogFrame);
	void Engage();
	void Disengage();

private:
	bool Load(const string& szFilePath);
	bool Execute();
	string ConvertFrameToCommand(MoogFrame* commandFrame);
};

//-------------------------------------------------------------------
// END OF FILE
//-------------------------------------------------------------------
