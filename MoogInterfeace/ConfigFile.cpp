// tabs=4
//************************************************************
//	COPYRIGHT 2003 Moog Inc. - ALL RIGHTS RESERVED
//
// This file is the product of Moog Inc. and cannot be 
// reproduced, copied, or used in any shape or form without 
// the express written consent of Moog Inc.
//************************************************************
//
//	$Source: /srv/cvs/EDL/TestCode/WinMotion_Base_Host/ConfigFile.cpp,v $
//
//	Reason: Implement the methods to read and write an
//			.ini file.
//
//	Revision History: See end of file.
//
//*************************************************************
#ifndef CConfigFileH
#define CConfigFileH


#include <windows.h>
// system includes
#include <string.h>		// define string manipulation functions

// local includes
#include "ConfigFile.h"		// define this class


// Class Constructor
//------------------------------------------------------------------------
CConfigFile::CConfigFile(const char* filename , std::ofstream* logger)
{
	// ensure we have room for the entire filename
	if (strlen(filename) > MAX_FILE_NAME_SIZE)
	{
		// Note: The following cannot be a CEvent because this class is used to 
		// construct the EventLog, and the EventLog is not yet available!
		// TRACE_ERR("CConfigFile::CConfigFile - Filename [%s] too long!\n", filename);
		printf("CConfigFile::CConfigFile - Filename [%s] too long!\n", filename);
		return;
	}

	// Get full pathname of file.
	strcpy(myPathname, filename);

	//	TRACE_INF("Config::myPathname[%s]", myPathname);

	// Get directory name of file.
	strcpy(myDirectory, filename);

	//	TRACE_INF("Config::myDirectory[%s]", myDirectory);
	char*	lastBackSlash = strrchr(myDirectory, int('\\'));
	if (lastBackSlash != NULL)
	{
		*lastBackSlash = NULL;

		// Get filename of file.
		lastBackSlash++;
		strcpy(myFilename, lastBackSlash);

		// open the file for reading and writing
		myFile = fopen(myPathname, "r");
		if (myFile == NULL)
		{
			// Note: The following cannot be a CEvent because this class is used to 
			// construct the EventLog, and the EventLog is not yet available!
			//TRACE_ERR("CConfigFile::CConfigFile - Could not open file [%s]", myPathname);
			printf("CConfigFile::CConfigFile - Could not open file [%s]", myPathname);
		}
	}
	else
	{
		myFile = NULL;
		// Note: The following cannot be a CEvent because this class is used to 
		// construct the EventLog, and the EventLog is not yet available!
		//TRACE_ERR("CConfigFile::CConfigFile - Could not open file [%s]", myPathname);
		printf("CConfigFile::CConfigFile - Could not open file [%s]", myPathname);
	}

	m_logger = logger;
}


// Class Destructor
//------------------------------------------------------------------------
CConfigFile::~CConfigFile(void)
{
	if (myFile != NULL)
	{
		fclose(myFile);
		myFile = NULL;
	}
}

bool
CConfigFile::DidFileOpen(void)
{
	if (myFile == NULL)
		return false;
	else
		return true;
}

// Read an INI file and determine if the section is there.
// If the section does not exist, the return value is false
//------------------------------------------------------------------------
bool
CConfigFile::IsAvailable(const char* section)
{
	// DEBUG: printf("CConfigFile::Read(%s)\n", section);

	// define a buffer to hold the data
	static char buffer[MAX_VALUE_SIZE];

	if (myFile != NULL)
	{
		// reset to the beginning of the file
		fseek(myFile, 0L, SEEK_SET);

		// search for the desired section
		while (fgets(buffer, MAX_VALUE_SIZE, myFile) != NULL)
		{
			// ignore comment lines
			if (buffer[0] == '#')
				continue;

			// see if we're at a section header
			if (buffer[0] == '[')
			{
				// see if we're at the right section
				if (strstr(buffer, section) != NULL)
					return(true);
			}
		}
	}
	else
	{
		printf("\nConfig File not found!");
	}

	return (false);
}


// Read the value for a key from an INI file from the specified section.
// If the section or key do not exist, the return value is NULL?
// NOTE: The caller MUST copy the results out of the static buffer!
//------------------------------------------------------------------------
const char*
CConfigFile::Read(const char* section, const char* key)
{
	// DEBUG: printf("CConfigFile::Read(%s, %s)\n", section, key);

	// define a buffer to hold the data

	if (myFile == NULL)
	{
		printf("\nConfig File not found!", section, key);
		return NULL;
	}

	// reset to the beginning of the file
	fseek(myFile, 0L, SEEK_SET);

	// search for the desired section
	while (fgets(buffer, MAX_VALUE_SIZE, myFile) != NULL)
	{
		// ignore comment lines
		if (buffer[0] == '#')
			continue;

		// see if we're at a section header
		if (buffer[0] == '[')
		{
			// see if we're at the right section
			if (strstr(buffer, section) != NULL)
			{
				// search for the desired key
				while (fgets(buffer, MAX_VALUE_SIZE, myFile) != NULL)
				{
					// ignore comment lines
					if (buffer[0] == '#')
						continue;

					// if we reach a section head, we've failed
					if (buffer[0] == '[')
					{
						printf("\nSection: %s, Key: %s not found!", section, key);
						return NULL;
					}

					// extract the token
					char* token = strtok(buffer, "=");
					// compare for an exact match
					if ((token != NULL) && (strncmp(token, key, strlen(key)) == 0))
					{
						// extract the desired value (ignore trailing comments or spaces)
						token = strtok(NULL, "\t#\n\r");
						if (token != NULL)
						{
							strcpy(value, token);
							return value;
						}
					}
				}
			}
		}
	}

	printf("\nSection: %s, Key: %s not found!", section, key);

	return NULL;
}


// Read the value for a key from an INI file from the specified section.
// If the section or key do not exist, the return value is the passed defaultValue.
// NOTE: The caller MUST copy the results out of the static buffer!
//------------------------------------------------------------------------
const char*
CConfigFile::Read(const char* section, const char* key, const char* defaultValue)
{
	// DEBUG: printf("CConfigFile::Read(%s, %s, %s)\n", section, key, default);
#ifdef _WINDOWS
	LPCTSTR lpAppName;        // points to section name
	LPCTSTR lpKeyName;        // points to key name
	LPCTSTR lpDefault;        // points to default string
	LPTSTR lpReturnedString;  // points to destination buffer
	DWORD nSize;              // size of destination buffer
	LPTSTR lpFileName;		  // points to initialization filename

	lpAppName = section;
	lpKeyName = key;
	lpDefault = defaultValue;
	lpReturnedString = &value[0];
	nSize = MAX_VALUE_SIZE;
	lpFileName = &myPathname[0];

	GetPrivateProfileString(lpAppName, lpKeyName, lpDefault, lpReturnedString, nSize, lpFileName);

	return lpReturnedString;

#else
	if (myFile != NULL)
	{
		// reset to the beginning of the file
		fseek(myFile, 0L, SEEK_SET);

		// search for the desired section
		while (fgets(buffer, MAX_VALUE_SIZE, myFile) != NULL)
		{
			// ignore comment lines
			if (buffer[0] == '#')
				continue;

			// see if we're at a section header
			if (buffer[0] == '[')
			{
				// see if we're at the right section
				if (strstr(buffer, section) != NULL)
				{
					// search for the desired key
					while (fgets(buffer, MAX_VALUE_SIZE, myFile) != NULL)
					{
						// ignore comment line
						if (buffer[0] == '#')
							continue;

						// if we reach a section head, we've failed
						if (buffer[0] == '[')
						{
							return defaultValue;
						}

						// extract the token
						char* token = strtok(buffer, "=");
						// compare for an exact match
						if ((token != NULL) && (strncmp(token, key, strlen(key)) == 0))
						{
							// extract the desired value (ignore trailing comments or spaces)
							//token = strtok(NULL, " \t#\n\r");
							// extract the desired value
							token = strtok(NULL, "\t#\n\r");
							if (token != NULL)
							{
								strcpy(value, token);
								return value;
							}
						}
					}
				}
			}
		}
	}

	return defaultValue;
#endif
}


// Write the value for the specified key to the section of the INI file.
//------------------------------------------------------------------------
bool
CConfigFile::Write(const char* section, const char* key, const char* value)
{

#ifdef _WINDOWS
	LPCTSTR lpAppName;   // pointer to section name
	LPCTSTR lpKeyName;   // pointer to key name
	LPCTSTR lpString;    // pointer to string to add
	LPCTSTR lpFileName;  // pointer to initialization filename

	lpAppName = section;
	lpKeyName = key;
	lpString = value;    // pointer to string to add
	lpFileName = &myPathname[0];

	BOOL bResult = WritePrivateProfileString(lpAppName, lpKeyName, lpString, lpFileName);
	if (bResult)
		return(true);
	else
		return(false);
#else
	return(true);
#endif
}

//-------------------------------------------------------------------
// Reads a line of text from the config file, stops reading at newline.
// Newline remains at end of string.
//
// Input:
//      szDestination   - destination buffer
//      iMaxLen         - max length of line (including NULL terminator)
//      
// Return:
//      Number of characters written to szDestination, including
//      NULL terminator. 0 if no data written.
//--------------------------------------------------------------------
int CConfigFile::ReadLine(char* szDestination, int iMaxLen)
{
	int iLength = 0;

	if (fgets(szDestination, iMaxLen, myFile) != NULL)
	{
		// get length
		iLength = strlen(szDestination);
		// remote the newline
		szDestination[iLength - 1] = '\0';
	}

	return iLength;
}

#endif
//---------------------------------------------------------------
//	Revision History:
//
//	$Log: ConfigFile.cpp,v $
//	Revision 1.1  2011/06/28 17:46:08  tgreier
//	Initial Version.
//
//	Revision 1.3  2010/05/10 16:27:50  tgreier
//	ES00032609 (Add 6DOF2000) - Separated trace messages from fault messages, created fault table to map fault codes into fault reactions, instead
//	of being done explicitly. This allows different systems to assign different fault reactions to fault codes.
//	Changed CEvent::Create() to use TRACE_xxx macros for traces, FAULT_SET/CLEAR() macros for faults.
//
//	Revision 1.2  2010/02/16 17:00:10  tgreier
//	Implemented script files.
//
//	Revision 1.1  2010/02/09 20:46:28  tgreier
//	Initial version.
//
//
//---------------------------------------------------------------



