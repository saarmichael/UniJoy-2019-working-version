// tabs=4
//************************************************************
//	COPYRIGHT 2003 Moog Inc. - ALL RIGHTS RESERVED
//
// This file is the product of Moog Inc. and cannot be 
// reproduced, copied, or used in any shape or form without 
// the express written consent of Moog Inc.
//************************************************************
//
//	$Source: /srv/cvs/EDL/TestCode/WinMotion_Base_Host/ConfigFile.hpp,v $
//
//	Reason: Define a class to access configuration 
//			values in an .ini file.
//
//	Revision History: See end of file.
//
//*************************************************************

#ifndef CONFIG_FILE_HPP
#define CONFIG_FILE_HPP

static const char config_file_hpp_id[] = "$Id: ConfigFile.hpp,v 1.1 2011/06/28 17:46:08 tgreier Exp $";

// system includes
#include <stdio.h>		// printf, NULL, etc.

//local includes
#include "Configuration.h"	// define our base class

#include <fstream>


// constants

//--------------------------------------------------------------
class CConfigFile : public CConfiguration			// declare the class
{
public:
	CConfigFile(const char* filename , std::ofstream* logger);	// constructor
	bool        IsAvailable(const char* section);  // Determine if section is present
	const char* Read(const char* section, const char* key);	// client MUST copy return results
	const char* Read(const char* section, const char* key, const char* defaultValue);	// client MUST copy return results
	bool Write(const char* section, const char* key, const char* value);

	const char* GetFilename(void)	{	return myFilename;	};
	const char* GetPathname(void)	{	return myPathname;	};
	const char* GetDirectory(void)	{	return myDirectory;	};
	bool DidFileOpen(void);

    int ReadLine(char* szDestination, int iMaxLen);

	//Get the Moog main logger.
	std::ofstream* GetLogger(){return m_logger;}

	~CConfigFile(void);		// destructor

private:
	enum {
		MAX_VALUE_SIZE = 1024,		// buffer size for reading values
		MAX_FILE_NAME_SIZE = 256	// filename size
	};

	CConfigFile(void);		// constructor
	CConfigFile(const CConfigFile& object);		// default copy constructor
	CConfigFile& operator=(const CConfigFile& object);	// default assignment operator

	// other private data and methods
	bool isOk;				// true if registry has been properly opened
	char myFilename[MAX_FILE_NAME_SIZE];	// INI filename to read / write
	char myPathname[MAX_FILE_NAME_SIZE];	// Full pathname of INI file
	char myDirectory[MAX_FILE_NAME_SIZE];	// Directory where file resides
	FILE* myFile;		// pointer to the opened file

	char buffer[MAX_VALUE_SIZE];
	char value[MAX_VALUE_SIZE];

	std::ofstream* m_logger;
};

#endif	// CONFIG_FILE_HPP


//---------------------------------------------------------------
//	Revision History:
//
//	$Log: ConfigFile.hpp,v $
//	Revision 1.1  2011/06/28 17:46:08  tgreier
//	Initial Version.
//
//	Revision 1.2  2010/02/16 17:00:10  tgreier
//	Implemented script files.
//
//	Revision 1.1  2010/02/09 20:46:28  tgreier
//	Initial version.
//
//	Revision 1.2  2009/11/16 17:01:59  tgreier
//	Moved code from SPL\90430 (CA90430_RELEASE_R001).
//
//	Revision 1.1  2009/09/28 14:49:47  tgreier
//	Moved code from EDL\90430 (CA90430_3DOF_End_Of_Dev to SPL\90430 (CA90430R001_FQT_Iteration_1).
//
//	Revision 1.1  2009/05/05 12:38:30  pmarzec
//	Moving released code CA73408R003 from SPL into EDL for further development. This will serve as the baseline for the start of the xDOF motion platoform PC software.
//
//	Revision 1.1  2008/07/23 14:10:24  pmarzec
//	SPL Check in.
//
//	Revision 1.1  2008/05/19 20:55:04  pmarzec
//	SPL Check in pre FQT.
//
//	Revision 1.1  2008/05/12 14:50:27  pmarzec
//	EDL checkin under CA73408 part number
//
//	Revision 1.2  2007/12/11 14:36:46  pmarzec
//	Merged from R004 branch (up to E034) into HEAD line.
//
//	Revision 1.1.4.1  2007/11/05 18:54:43  pmarzec
//	Added function to check if file opened.
//
//	Revision 1.1  2007/06/01 17:56:21  pmarzec
//	CA27692E027
//
// 
//    Rev 1.0   Jun 22 2006 16:58:34   pmarzec
// Initial revision.
// 
//    Rev 1.0   Oct 21 2005 16:12:44   pmarzec
// Initial revision.
//
//2     1/13/05 3:12p Buckleyj
//Added GetFilename(), GetDirectory(), and GetPathname() of configuration
//file.
//
//1     12/02/04 11:04a Buckleyj
// 
//    Rev 1.1   Aug 24 2004 13:14:14   bshete
// Corrected the comment prefix put by PVCS from " *" to "//"
// 
//    Rev 1.0   Jul 05 2004 15:34:04   icdsftad
// Initial revision.
//	Revision 1.7  2004/05/17 22:40:24  tomb
//	Added a method to test whether a section is available or not.
//	
//	Revision 1.6  2004/05/17 18:30:13  bfriedman
//	Added test to see if only a section exists
//	
//	Revision 1.5  2004/04/23 15:04:24  tomb
//	Changed filename parameter to const char*.
//	
//	Revision 1.4  2004/04/20 18:32:16  tomb
//	Added method to return the specified default if an entry is not found.
//	
//	Revision 1.3  2004/03/19 14:59:16  tomb
//	Removed UNDER_RTSS #def, removed #includes where not needed.
//	
//	Revision 1.2  2004/02/16 21:27:29  tomb
//	Updated include files for RTX.
//	
//	Revision 1.1  2004/01/29 15:21:10  tomb
//	Copied and ported to RTX from New6DOF.
//	
//	Revision 1.1  2004/01/12 17:36:56  tomb
//	Transferred from common.
//	
//	Revision 1.1  2004/01/12 13:54:37  tomb
//	Initial Checkin.
//	
