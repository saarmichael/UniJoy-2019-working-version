#ifdef __cplusplus
extern "C" {
#endif

	typedef struct
	{
		int iEnum;
		const char* szString;
	} TConvertEnum;


	int ConvertStrToEnum(const char* szToken, TConvertEnum* pTable);
	const char* ConvertEnumToStr(int eEnum, TConvertEnum* pTable);

#ifdef __cplusplus
}
#endif

