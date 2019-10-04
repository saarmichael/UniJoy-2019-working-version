// tabs=4
//************************************************************
//	COPYRIGHT 2003 Moog Inc. - ALL RIGHTS RESERVED
//
// This file is the product of Moog Inc. and cannot be 
// reproduced, copied, or used in any shape or form without 
// the express written consent of Moog Inc.
//************************************************************
//
//	$Source: /srv/cvs/EDL/TestCode/WinMotion_Base_Host/Configuration.hpp,v $
//
//	Reason: Define an abstract interface to persistent 
//			configuration data.
//
//	Revision History: See end of file.
//
//*************************************************************

#ifndef CONFIGURATION_HPP
#define CONFIGURATION_HPP

static const char configuration_hpp_id[] = "$Id: Configuration.hpp,v 1.1 2011/06/28 17:46:08 tgreier Exp $";

// system includes

//local includes

// constants

//--------------------------------------------------------------
class CConfiguration			// declare the class
{
public:
	virtual bool IsAvailable(const char* folder) { return false; }  // Determine if section is present
	virtual const char* Read(const char* folder, const char* key) = 0;	// client MUST copy return results
	virtual const char* Read(const char* folder, const char* key, const char* defaultValue) = 0;	// client MUST copy return results
	virtual bool Write(const char* folder, const char* key, const char* value) = 0;

protected:
	CConfiguration(void) {}		// constructor
	virtual ~CConfiguration(void) {}	// destructor

private:
	CConfiguration(const CConfiguration& object);		// default copy constructor
	CConfiguration& operator=(const CConfiguration& object);	// default assignment operator

};

#endif	// CONFIGURATION_HPP


//---------------------------------------------------------------
//	Revision History:
//
//	$Log: Configuration.hpp,v $
//	Revision 1.1  2011/06/28 17:46:08  tgreier
//	Initial Version.
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
//1     12/02/04 11:04a Buckleyj
// 
//    Rev 1.1   Aug 24 2004 13:14:14   bshete
// Corrected the comment prefix put by PVCS from " *" to "//"
// 
//    Rev 1.0   Jul 05 2004 15:34:04   icdsftad
// Initial revision.
//	Revision 1.4  2004/05/17 22:40:24  tomb
//	Added a method to test whether a section is available or not.
//	
//	Revision 1.3  2004/05/17 18:30:13  bfriedman
//	Added test to see if only a section exists
//	
//	Revision 1.2  2004/04/20 18:32:16  tomb
//	Added method to return the specified default if an entry is not found.
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
