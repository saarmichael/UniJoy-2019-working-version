// tabs=4
//************************************************************
//	COPYRIGHT 2010 Moog Inc. - ALL RIGHTS RESERVED
//
// This file is the product of Moog Inc. and cannot be 
// reproduced, copied, or used in any shape or form without 
// the express written consent of Moog Inc.
//************************************************************
//
//  Defines C-functions that output text to log.
//  Outputs to stdout .
//
//	Revision History: See end of file.
//
//*************************************************************

#include <stdio.h>
#include <stdarg.h>
#include "LogOutput.h"
#include <windows.h>


//---------------------------------------------------------
// Print variable argument data.
//---------------------------------------------------------
void LogWriteV(const char* format, va_list vargs)
{
	vprintf(format, vargs);
}

//---------------------------------------------------------
// Print text, no newline or timestamp.
//---------------------------------------------------------
void LogWrite(const char* format, ...)
{
	va_list argptr;
	va_start(argptr, format);

	/* print the string */
	vprintf(format, argptr);

	/* End the variable format section of this presentation... */
	va_end(argptr);
}

//---------------------------------------------------------
// Print text, preceded by newline and timestamp.
//---------------------------------------------------------
void LogWriteLn(const char* format, ...)
{
	unsigned long ulSysTimeMs = 0;

	ulSysTimeMs = GetTickCount();

	va_list argptr;
	va_start(argptr, format);

	/* print new line and time stamp (HH:MM:SS:MS) */
	unsigned int hours = ulSysTimeMs / 3600000;
	ulSysTimeMs -= (hours * 3600000);
	unsigned int minutes = ulSysTimeMs / 60000;
	ulSysTimeMs -= (minutes * 60000);
	unsigned int seconds = ulSysTimeMs / 1000;
	ulSysTimeMs -= (seconds * 1000);
	printf("\n[%02d:%02d:%02d:%03d] ", hours, minutes, seconds, ulSysTimeMs);

	/* print the string */
	vprintf(format, argptr);

	/* End the variable format section of this presentation... */
	va_end(argptr);
}

//-------------------------------------------------------------------
// END OF FILE
//-------------------------------------------------------------------
