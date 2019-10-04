#include "ConvertEnum.h"
#include <stdio.h>
#include <string.h>

//---------------------------------------------------------
// Converts from a string to an enumeration integer.
//---------------------------------------------------------
int ConvertStrToEnum(const char* szToken, TConvertEnum* pTable)
{
	int i;

	for (i = 0; pTable[i].szString != NULL; i++)
	{
		if (_stricmp(szToken, pTable[i].szString) == 0)
		{
			return pTable[i].iEnum;
		}
	}

	// last item should be default.
	return pTable[i].iEnum;
}

//---------------------------------------------------------
// Converts from an enumeration integer to a string.
//---------------------------------------------------------
const char* ConvertEnumToStr(int eEnum, TConvertEnum* pTable)
{
	int i;

	for (i = 0; pTable[i].szString != NULL; i++)
	{
		if (eEnum == pTable[i].iEnum)
		{
			return pTable[i].szString;
		}
	}

	return "Unknown";
}

