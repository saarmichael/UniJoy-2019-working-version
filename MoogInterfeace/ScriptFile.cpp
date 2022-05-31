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
//
// Any Mode Commands:
// -------------------------
// MODE <Length|DOF|MDA|Playback> [playback file id]
// ENGAGE
// DISENGAGE
// RESET
// PAUSE <seconds> [IgnoreFaults]
// SIGNAL None
// SIGNAL <Roll|Pitch|Yaw|Long|Lat|Heave|Length> <Sine|Triangle> <Amplitude> <Offset> <RateHz>
// LENGTH <all actuator length, meters>
// WAITFORSTATE <Power-Up|Ready|Standby|Engaged|Settling|Fault-1|Fault-2|Fault-3|Disabled|Inhibited|Frozen> <seconds> [IgnoreFaults]
// SKIP <num commands to MBC>
//
// DOF/MDA Mode Commands:
// --------------------------
// BUFFET_OPT3 <id 1-10> <freqHz> <amplitude X> <amplitude Y> <amplitude Z>
// BUFFET_OPT8 <id 1-10> <freqHz> <amplitude X> <amplitude Y> <amplitude Z>
//
// DOF Mode Commands:
// --------------------------
// DOF <pitch|roll|yaw|long|lat|heave> <value in rads or meters>
// WHITENOISE_OPT4 <id 1-2> <power X> <power Y> <power Z> 
// WHITENOISE_OPT7 <id 1-2> <power X> <hp Hz> <lp Hz> <power Y>  <hp Hz> <lp Hz> <power Z> <hp Hz> <lp Hz> 
//
// MDA Mode Commands:
// -------------------------
// MDA freeze <1|0>
// MDA <roll|pitch|yaw> <radians>
// MDA <rollRate|pitchRate|yawRate> <radians/sec>
// MDA <rollAcc|pitchAcc|yawAcc> <radians/sec2>
// MDA <longAcc|latAcc|HeaveAcc> <meters/sec2>
//
// Playback Mode Commands:
// -------------------------
// PLAYSTART
//--------------------------------------------------------------


#include "ScriptFile.h"


static TConvertEnum sg_SignalGenAxisConvert[] =
{ { CMBCInterface::eSIGNALGEN_AXIS_NONE, "None" },
{ CMBCInterface::eSIGNALGEN_AXIS_ROLL, "Roll" },
{ CMBCInterface::eSIGNALGEN_AXIS_PITCH, "Pitch" },
{ CMBCInterface::eSIGNALGEN_AXIS_YAW, "Yaw" },
{ CMBCInterface::eSIGNALGEN_AXIS_LONGITUDINAL, "Long" },
{ CMBCInterface::eSIGNALGEN_AXIS_LATITUDINAL, "Lat" },
{ CMBCInterface::eSIGNALGEN_AXIS_HEAVE, "Heave" },
{ CMBCInterface::eSIGNALGEN_AXIS_LENGTH, "Length" },
{ CMBCInterface::eSIGNALGEN_AXIS_NONE, NULL } };

static TConvertEnum sg_SignalGenWaveConvert[] =
{ { CSignalGenerator::eWAVEFORM_TRIANGLE, "Triangle" },
{ CSignalGenerator::eWAVEFORM_SINE, "Sine" },
{ CSignalGenerator::eWAVEFORM_NONE, NULL } };

static TConvertEnum sg_DofAxisConvert[] =
{ { CMBCInterface::eDOF_AXIS_ROLL, "Roll" },
{ CMBCInterface::eDOF_AXIS_PITCH, "Pitch" },
{ CMBCInterface::eDOF_AXIS_YAW, "Yaw" },
{ CMBCInterface::eDOF_AXIS_LONGITUDINAL, "Long" },
{ CMBCInterface::eDOF_AXIS_LATITUDINAL, "Lat" },
{ CMBCInterface::eDOF_AXIS_HEAVE, "Heave" },
{ CMBCInterface::eDOF_AXIS_NONE, NULL } };

static TConvertEnum sg_CommandTable[] =
{
	{ CScriptFile::COMMAND_ENGAGE, "Engage" },
	{ CScriptFile::COMMAND_DISENGAGE, "Disengage" },
	{ CScriptFile::COMMAND_PAUSE, "Pause" },
	{ CScriptFile::COMMAND_SIGNALGEN, "Signal" },
	{ CScriptFile::COMMAND_WAITFORSTATE, "WaitForState" },
	{ CScriptFile::COMMAND_RESET, "Reset" },
	{ CScriptFile::COMMAND_MODE, "Mode" },
	{ CScriptFile::COMMAND_LENGTH, "Length" },
	{ CScriptFile::COMMAND_DIRECT_DISPLACEMENT_DOF, "DD_DOF_Opt1" },
	{ CScriptFile::COMMAND_DIRECT_DISPLACEMENT_LENGTH, "DD_Len_Opt2" },
	{ CScriptFile::COMMAND_BUFFET_DISPLACEMENT, "Buffet_Opt3" },
	{ CScriptFile::COMMAND_BUFFET_ACCELERATION, "Buffet_Opt8" },
	{ CScriptFile::COMMAND_WHITENOISE_DISPLACEMENT, "WhiteNoise_Opt4" },
	{ CScriptFile::COMMAND_WHITENOISE_ACCELERATION, "WhiteNoise_Opt7" },
	{ CScriptFile::COMMAND_DOF, "Dof" },
	{ CScriptFile::COMMAND_MDA, "Mda" },
	{ CScriptFile::COMMAND_SKIP, "Skip" },
	{ CScriptFile::COMMAND_PLAYSTART, "PlayStart" },
	{ CScriptFile::COMMAND_INVALID, NULL }
};

static TConvertEnum sg_CommandMode[] =
{ { CMBCInterface::eCMD_MODE_LENGTH, "Length" },
{ CMBCInterface::eCMD_MODE_DOF, "DOF" },
{ CMBCInterface::eCMD_MODE_MDA, "MDA" },
{ CMBCInterface::eCMD_MODE_PLAYBACK, "Playback" },
{ CMBCInterface::eCMD_MODE_NONE, NULL }
};


static TConvertEnum sg_MdaParameters[] =
{
	{ CMBCInterface::eMDA_PARAM_ROLL, "Roll" },
	{ CMBCInterface::eMDA_PARAM_PITCH, "Pitch" },
	{ CMBCInterface::eMDA_PARAM_YAW, "Yaw" },
	{ CMBCInterface::eMDA_PARAM_ROLLRATE, "RollRate" },
	{ CMBCInterface::eMDA_PARAM_PITCHRATE, "PitchRate" },
	{ CMBCInterface::eMDA_PARAM_YAWRATE, "YawRate" },
	{ CMBCInterface::eMDA_PARAM_ROLLACC, "RollAcc" },
	{ CMBCInterface::eMDA_PARAM_PITCHACC, "PitchAcc" },
	{ CMBCInterface::eMDA_PARAM_YAWACC, "YawAcc" },
	{ CMBCInterface::eMDA_PARAM_LONGACC, "LongAcc" },
	{ CMBCInterface::eMDA_PARAM_LATACC, "LatAcc" },
	{ CMBCInterface::eMDA_PARAM_HEAVEACC, "HeaveAcc" },
	{ CMBCInterface::eMDA_PARAM_FREEZE, "Freeze" },
	{ CMBCInterface::eMDA_PARAM_NONE, NULL },
};




//---------------------------------------------------------
// Constructor.
//---------------------------------------------------------
CScriptFile::CScriptFile(CMBCInterface* pMBC , ofstream* logFile)
{
	m_mbcInterface = pMBC;
	m_logFile = logFile;

	m_iNumCommands = 0;
	//avi : for starting from the beginning of the script.
	m_iCommandIndex = 0;
	//avi : for resteing the values of all actuators.
	m_mbcInterface->ResetCommands();

	memset((void*)m_stCommands, 0, sizeof(m_stCommands));

	SetPriorityClass(GetCurrentProcess(), REALTIME_PRIORITY_CLASS);
}

//---------------------------------------------------------
// Destructor.
//---------------------------------------------------------
CScriptFile::~CScriptFile()
{
}

#include <string>
using namespace std;

int ReadLineInCommandStrings(char* buffer, const string& commandsString, int& placeInCommandStrings, ofstream& logFile)
{
	logFile << "ReadingLine\n";
	logFile.flush();
    // ~(Michael Saar)~ finds the first '\n' in the substring stating at placeInCommandStrings   
	int numOfCharsToRead = commandsString.find('\n', placeInCommandStrings);
	// ~(Michael Saar)~ if no '\n' was found ?? is it the end of the script ??
	if (numOfCharsToRead == -1)
		return 0;

	logFile << "Substring...\n";
	logFile.flush();

	string s = commandsString.substr(placeInCommandStrings, numOfCharsToRead - placeInCommandStrings);

	logFile << "placeInCommandStrings " << placeInCommandStrings << "numOfCharsToRead - placeInCommandStrings" << numOfCharsToRead - placeInCommandStrings << "\n";
	logFile.flush();

	logFile << "End Substring\n";
	logFile.flush();

	logFile << s << "\n";
	logFile.flush();

	logFile << "Memcpy...\n";
	logFile.flush();

	logFile << "numOfCharsToRead " << numOfCharsToRead << "\n";
	logFile.flush();

	memcpy(buffer, &s[0], numOfCharsToRead - placeInCommandStrings);

	logFile << "End of memcpy\n";
	logFile.flush();

	//strcpy(buffer, s.c_str());

	logFile << "Buffer[numOfCharsToRead] = 0 ...\n";
	logFile.flush();

	buffer[numOfCharsToRead - placeInCommandStrings] = '\0';

	logFile << "End buffer[numOfCharsToRead] = 0\n";
	logFile.flush();

	placeInCommandStrings += (numOfCharsToRead - placeInCommandStrings + 1);
	
	logFile << "End reading line\n";
	logFile.flush();


	return numOfCharsToRead;
}

bool CScriptFile::Load(const string& commandString)
{
#define MAX_LINE_LENGTH 1000
	char szBuffer[MAX_LINE_LENGTH + 1];       // buffer for a line of data from script file
	//CConfigFile scriptFile(szFilePath);     // script file object


	//avi : for not making the buffer with limit for only some trials (and thean the buffer go over).
	m_iCommandIndex = 0;
	m_iNumCommands = 0;
	memset((void*)m_stCommands, 0, sizeof(m_stCommands));



	int iLineNumber = 0;
	if (0)
	{

	}
	else
	{
		int placeIncommandsString = 0;
		// read each line and look for commands
		//while ((scriptFile.ReadLine(szBuffer, MAX_LINE_LENGTH) > 0) &&
		while ((ReadLineInCommandStrings(szBuffer, commandString, placeIncommandsString , *m_logFile) > 0) &&
			(m_iNumCommands < eMAX_NUM_COMMANDS))
		{
			char* szContext = NULL;
			char* szToken = NULL;

			iLineNumber++;

			// tokenize the string and look for commands/arguments
			if ((szToken = strtok_s(szBuffer, " ", &szContext)) != NULL)
			{
				TCommand* pCommand = NULL;  // non-NULL if a command line is found
				TECommand eCommand = COMMAND_INVALID;

				// see if it is a command
				eCommand = (TECommand)ConvertStrToEnum(szToken, sg_CommandTable);
				if (eCommand != COMMAND_INVALID)
				{
					pCommand = &m_stCommands[m_iNumCommands++];
					pCommand->eCommand = eCommand;
					pCommand->iLineNumber = iLineNumber;

					// add command string as first arg
					strcpy_s(pCommand->szArgs[pCommand->iNumArgs++], eMAX_NUM_COMMAND_ARGUMENT_LENGTH, szToken);
				}

				// if got a command, then add arguments
				if (pCommand != NULL)
				{
					while (((szToken = strtok_s(NULL, " ", &szContext)) != NULL) &&
						(pCommand->iNumArgs < eMAX_NUM_COMMAND_ARGUMENTS))
					{
						strcpy_s(pCommand->szArgs[pCommand->iNumArgs++], eMAX_NUM_COMMAND_ARGUMENT_LENGTH, szToken);
					}
				}
			}  // if got token on the line

		} // while reading lines
	}

	return true;
}

//-------------------------------------------------------------------
// Executes the script, starting at the beginning
//
// Return:
//      true if script ran successfully, false if error occurred.
//-------------------------------------------------------------------
bool CScriptFile::Execute()
{
	bool bFault = false;

	// start from beginning of script

	//avi : dont begin from the start.
	//m_iCommandIndex = 0;

	// Reset the MBC interface parameters in order to start the script
	// from a clean slate of commands

	//avi : deleted for not erasing the values of the last commands (such as command for heave set and then long set , the heavse set is cleared here - something not wanted here.
	//placed into the initialization of the constructor.
	//pMBC->ResetCommands();


	//avi : interpolation frequency , and that the commands for 6 points are in 6 commands and not in true one command line.
	//Sleep(1);
	//std::this_thread::sleep_for(std::chrono::microseconds(520));
	//std::this_thread::sleep_for(std::chrono::microseconds(300));
	//std::this_thread::sleep_for(std::chrono::nanoseconds(520933)); // thr real value
	Sleep(1);

	// execute each script command
	while ((m_iCommandIndex <  m_iNumCommands) && (!bFault))
	{
		//avi : real sleep between messages.
		//Sleep(1);

		TCommand* pCommand = &m_stCommands[m_iCommandIndex++];

		LogWriteLn("%d:", pCommand->iLineNumber);
		for (int i = 0; i < pCommand->iNumArgs; i++)
			LogWrite("%s ", pCommand->szArgs[i]);

		switch (pCommand->eCommand)
		{
		case COMMAND_MODE:          // MODE <Length|DOF|MDA|Playback> [playback file id]
		{
			unsigned int uiPlaybackFileId = 0;

			CMBCInterface::TECommandMode eMode = (CMBCInterface::TECommandMode)ConvertStrToEnum(pCommand->szArgs[1], sg_CommandMode);

			if (pCommand->iNumArgs >= 3)
			{
				uiPlaybackFileId = atoi(pCommand->szArgs[2]);
			}

			m_mbcInterface->SetCommandMode(eMode, uiPlaybackFileId);
		}
		break;

		case COMMAND_ENGAGE:        // ENGAGE
			m_mbcInterface->SetMotionCommand(MOTION_CMD_ENGAGE);
			break;

		case COMMAND_DISENGAGE:     // DISENGAGE
			m_mbcInterface->SetMotionCommand(MOTION_CMD_DISENGAGE);
			break;

		case COMMAND_RESET:         // RESET
			m_mbcInterface->SetMotionCommand(MOTION_CMD_RESET);
			break;

		case COMMAND_PAUSE:         // PAUSE <seconds pause> [IgnoreFaults]
			if (pCommand->iNumArgs >= 2)
			{
				double dSeconds = atof(pCommand->szArgs[1]);
				bool bExitOnFault = true; // default to exit if a fault is received.

				if (pCommand->iNumArgs >= 3)
				{
					bExitOnFault = (_stricmp(pCommand->szArgs[2], "IgnoreFaults") == 0) ? false : true;
				}

				// if not exiting on a received fault, then just do a straight thread sleep.
				if (!bExitOnFault)
				{
					Sleep((DWORD)(dSeconds * 1000));
				}
				else
					// if exiting if a fault occurs, then wait for fault state to occur.
				{
					m_mbcInterface->WaitForState(EM_STATE_FAULT_1, (int)(dSeconds * 1000));

					// If fault has occurred, then exit the script.
					if (m_mbcInterface->InFaultState())
					{
						LogWriteLn("Faulted: Exitting Script.");
						return false;
					}
				}
			}
			break;

		case COMMAND_SIGNALGEN:     // SIGNAL None
			// SIGNAL <Roll|Pitch|Yaw|Long|Lat|Heave|Length> <Sine|Triangle|Square> <Amplitude> <Offset> <RateHz>
			if (pCommand->iNumArgs >= 6)
			{
				CMBCInterface::TESignalAxis eAxis = (CMBCInterface::TESignalAxis)ConvertStrToEnum(pCommand->szArgs[1], sg_SignalGenAxisConvert);
				CSignalGenerator::TWaveForm eWaveForm = (CSignalGenerator::TWaveForm)ConvertStrToEnum(pCommand->szArgs[2], sg_SignalGenWaveConvert);

				// initialize the signal generator
				m_mbcInterface->SetSignalGenerator(eAxis,                      // signal axis
					eWaveForm,                  // waveform shape
					atof(pCommand->szArgs[5]),  // freq
					atof(pCommand->szArgs[3]),  // amp
					atof(pCommand->szArgs[4])); // offset
			}
			else
			{
				if (_stricmp(pCommand->szArgs[1], "None") == 0)
				{
					m_mbcInterface->SetSignalGenerator(CMBCInterface::eSIGNALGEN_AXIS_NONE,
						CSignalGenerator::eWAVEFORM_NONE,
						0, 0, 0);
				}
			}
			break;

		case COMMAND_LENGTH:        // LENGTH <all actuator length, meters>
			m_mbcInterface->SetLengthPosition(atof(pCommand->szArgs[1]));
			break;

		case COMMAND_WAITFORSTATE:  // WAITFORSTATE <state> <seconds> [IgnoreFaults]
		{
			bool bExitOnFault = true;
			ENCODED_MACHINE_STATE_TYPE eState = EM_STATE_IDLE;
			double dSeconds = 0;

			if (pCommand->iNumArgs >= 3)
			{
				eState = m_mbcInterface->GetMachineStateEnum(pCommand->szArgs[1]);
				dSeconds = atof(pCommand->szArgs[2]);
			}
			if (pCommand->iNumArgs >= 4)
			{
				bExitOnFault = (_stricmp(pCommand->szArgs[3], "IgnoreFaults") == 0) ? false : true;
			}

			if (!m_mbcInterface->WaitForState(eState, (int)(dSeconds * 1000), bExitOnFault))
			{
				return false;
			}
		}
		break;

		case COMMAND_DOF: // DOF <pitch|roll|yaw|long|lat|heave> <value in rads or meters>
			if (pCommand->iNumArgs >= 3)
			{
				m_mbcInterface->SetDofPosition((CMBCInterface::TEDofAxis)ConvertStrToEnum(pCommand->szArgs[1], sg_DofAxisConvert),
					atof(pCommand->szArgs[2]));
			}
			break;

		case COMMAND_DIRECT_DISPLACEMENT_DOF:    // DIRECT_DISPLACEMENT_DOF_OPT1 <roll Rad> <pitch Rad> <Yaw Rad> <Long Meters> <Lat Meters> <Heave Meters>
			if (pCommand->iNumArgs >= 6)
			{
				m_mbcInterface->SetDirectDisplacement(CMBCInterface::eDIRECT_DISPLACEMENT_MODE_DOF,
					atof(pCommand->szArgs[1]),  // <roll Rad>
					atof(pCommand->szArgs[2]),  // <pitch Rad> 
					atof(pCommand->szArgs[3]),  // <Yaw Rad> 
					atof(pCommand->szArgs[4]),  // <Long Meters> 
					atof(pCommand->szArgs[5]),	// <Lat Meters> 
					atof(pCommand->szArgs[6])); // <Heave Meters>
			}
			break;

		case COMMAND_DIRECT_DISPLACEMENT_LENGTH:    // DIRECT_DISPLACEMENT_LEN_OPT2 <A Meters> <B Meters> <C Meters> <D Meters> <E Meters> <F Meters>
			if (pCommand->iNumArgs >= 6)
			{
				m_mbcInterface->SetDirectDisplacement(CMBCInterface::eDIRECT_DISPLACEMENT_MODE_LENGTH,
					atof(pCommand->szArgs[1]),  // <A Meters>
					atof(pCommand->szArgs[2]),  // <B Meters> 
					atof(pCommand->szArgs[3]),  // <C Meters>
					atof(pCommand->szArgs[4]),  // <D Meters>
					atof(pCommand->szArgs[5]),	// <E Meters>
					atof(pCommand->szArgs[6])); // <F Meters>
			}
			break;

		case COMMAND_BUFFET_DISPLACEMENT:    // BUFFET_OPT3 <id 1-10> <freqHz> <amplitude X> <amplitude Y> <amplitude Z>
			if (pCommand->iNumArgs >= 6)
			{
				m_mbcInterface->SetBuffet(CMBCInterface::eBUFFET_MODE_DISPLACEMENT,
					atoi(pCommand->szArgs[1]),  // index (1-10)
					atof(pCommand->szArgs[2]),  // freqHz
					atof(pCommand->szArgs[3]),  // amp X
					atof(pCommand->szArgs[4]),  // amp Y
					atof(pCommand->szArgs[5])); // amp Z
			}
			break;

		case COMMAND_BUFFET_ACCELERATION:    // BUFFET_OPT8 <id 1-10> <freqHz> <amplitude X> <amplitude Y> <amplitude Z>
			if (pCommand->iNumArgs >= 6)
			{
				m_mbcInterface->SetBuffet(CMBCInterface::eBUFFET_MODE_ACCELERATION,
					atoi(pCommand->szArgs[1]),  // index (1-10)
					atof(pCommand->szArgs[2]),  // freqHz
					atof(pCommand->szArgs[3]),  // amp X
					atof(pCommand->szArgs[4]),  // amp Y
					atof(pCommand->szArgs[5])); // amp Z
			}
			break;

		case COMMAND_WHITENOISE_DISPLACEMENT:   // WHITENOISE_OPT4 <id 1-2> <power X> <power Y> <power Z>
			if (pCommand->iNumArgs >= 5)
			{
				m_mbcInterface->SetWhiteNoise(CMBCInterface::eWHITENOISE_MODE_DISPLACEMENT,
					atoi(pCommand->szArgs[1]),  // index (1-2)
					atof(pCommand->szArgs[2]),  // power X
					0,  // HP (Hz) X
					0,  // LP (Hz) X
					atof(pCommand->szArgs[3]),  // power Y
					0,  // HP (Hz) Y
					0,  // LP (Hz) Y
					atof(pCommand->szArgs[4]),  // power Z
					0,  // HP (Hz) Z
					0); // LP (Hz) Z
			}
			break;

		case COMMAND_WHITENOISE_ACCELERATION:   // WHITENOISE_OPT7 <id 1-2> <power X> <hp Hz> <lp Hz> <power Y>  <hp Hz> <lp Hz> <power Z> <hp Hz> <lp Hz> 
			if (pCommand->iNumArgs >= 11)
			{
				m_mbcInterface->SetWhiteNoise(CMBCInterface::eWHITENOISE_MODE_ACCELERATION,
					atoi(pCommand->szArgs[1]),  // index (1-2)
					atof(pCommand->szArgs[2]),  // power X
					atof(pCommand->szArgs[3]),  // HP (Hz) X
					atof(pCommand->szArgs[4]),  // LP (Hz) X
					atof(pCommand->szArgs[5]),  // power Y
					atof(pCommand->szArgs[6]),  // HP (Hz) Y
					atof(pCommand->szArgs[7]),  // LP (Hz) Y
					atof(pCommand->szArgs[8]),  // power Z
					atof(pCommand->szArgs[9]),  // HP (Hz) Z
					atof(pCommand->szArgs[10]));// LP (Hz) Z
			}
			break;

		case COMMAND_MDA:   // MDA <roll|pitch|yaw> <radians>
			// MDA <rollRate|pitchRate|yawRate> <radians/sec>
			// MDA <rollAcc|pitchAcc|yawAcc> <radians/sec2>
			// MDA <longAcc|latAcc|HeaveAcc> <meters/sec2>
			if (pCommand->iNumArgs >= 3)
			{
				m_mbcInterface->SetMda((CMBCInterface::TEMdaParam)ConvertStrToEnum(pCommand->szArgs[1], sg_MdaParameters),
					atof(pCommand->szArgs[2]));
			}
			break;

		case COMMAND_SKIP:  // SKIP <num commands>
			if (pCommand->iNumArgs >= 2)
			{
				m_mbcInterface->SkipCommands(atoi(pCommand->szArgs[1]));
			}
			break;

		case COMMAND_PLAYSTART:  // PLAYSTART
			m_mbcInterface->SetMotionCommand(MOTION_CMD_START);
			break;
		}
	}

	return true;
}

void CScriptFile::SendMBCAxesPositions(MoogFrame* moogFrame)
{
	string commandString = ConvertFrameToCommand(moogFrame);
#pragma region LOGS-COMMAND
	*m_logFile << "new command string";
	*m_logFile << commandString;
	m_logFile->flush();
#pragma endregion LOGS-COMMAND
	Load(commandString);
	Execute();
#pragma region LOGS_COMMAND
	*m_logFile << "end command string\n\n";
	m_logFile->flush();
#pragma endregion LOGS_COMMAND
}

void CScriptFile::Engage()
{
	string s = "Mode Dof\nDOF heave ";
	s += to_string(MOTION_BASE_CENTER);
	s += "\nMDOF long 0";
	s += "\nMDOF yaw 0";
	s += "\nMDOF pitch 0";
	s += "\nMDOF roll 0";
	s += "\nMDOF lat 0";
	s += "\nReset\nWaitForState Ready 10 IgnoreFaults\nEngage\nWaitForState Engaged 20\n";

	Load(s);
	Execute();
}

void CScriptFile::Disengage()
{
	string s = "Disengage\nWaitForState Ready 30 IgnoreFaults\n";

	Load(s);
	Execute();
}

string CScriptFile::ConvertFrameToCommand(MoogFrame* commandFrame)
{
	string commandString = "";

	commandString += DOF_LONG_COMMAND + to_string(commandFrame->surge) + "\n";
	commandString += DOF_HEAVE_COMMAND + to_string(commandFrame->heave) + "\n";
	commandString += DOF_LAT_COMMAND + to_string(commandFrame->lateral) + "\n";
	commandString += DOF_ROLL_COMMAND + to_string(commandFrame->roll) + "\n";
	commandString += DOF_PITCH_COMMAND + to_string(commandFrame->pitch) + "\n";
	commandString += DOF_YAW_COMMAND + to_string(commandFrame->yaw) + "\n";

	return commandString;
}


//-------------------------------------------------------------------
// END OF FILE
//-------------------------------------------------------------------
