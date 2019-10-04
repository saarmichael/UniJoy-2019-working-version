// tabs=4
//************************************************************
//	COPYRIGHT 2010 Moog Inc. - ALL RIGHTS RESERVED
//
// This file is the product of Moog Inc. and cannot be 
// reproduced, copied, or used in any shape or form without 
// the express written consent of Moog Inc.
//************************************************************
//
//  RTX Application that sends Host command packets to 
//  a motion base computer (MBC).
//
//  Opens configuration file: C:\Moog\Motion_Base_Host.ini
//
//	Revision History: See end of file.
//
//*************************************************************


#include "Motion_Base_Host.h"
#include "ConfigFile.h"
#include "MBC_Interface.h"
#include "SignalGenerator.h"
#include "ScriptFile.h"
#include "LogOutput.h"


CMBCInterface MBCIF;    // interface to Motion Base Computer


void _cdecl main
(
int  argc,
char **argv,
char **envp
)
{
	/*const char* pszParam = NULL;
	CScriptFile* pScriptFile = NULL;

	//The config object expects a full path name and file name eg. "C:/Moog/Motionbase.ini"
	char cFilePath[MAX_PATH + 1];
	strcpy_s(cFilePath, MAX_PATH, "C:\\Moog\\MotionBaseHost.ini");
	CConfigFile* config = new CConfigFile(cFilePath);
	if (NULL == config)
	{
		LogWriteLn("	ERROR: Can't create CConfigFile(%s).\n", cFilePath);
		ExitProcess(-1);
	}

	// Read Script configuration
	int iNumCycles = atoi(config->Read("Script", "Cycles", "1"));

	pszParam = config->Read("Script", "File", "");
	LogWriteLn("Script file [%s]", pszParam);
	//pScriptFile = new CScriptFile(&MBCIF);
	//pScriptFile->Load(pszParam);

	// Open the host interface device
	MBCIF.Open(config);

	// Run the script until cycles complete, or fault.
	for (int i = 0; i < iNumCycles; i++)
	{
		LogWriteLn("\nScript Cycle [%d] ****************************************", i + 1);

		if (!pScriptFile->Execute(&MBCIF))
		{
			break;
		}

	} // end of cycle


	delete config;

	MBCIF.Close();

	delete pScriptFile;

	LogWriteLn("Done.");
	ExitProcess(0);*/
}


//-------------------------------------------------------------------
// END OF FILE
//-------------------------------------------------------------------






