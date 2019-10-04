// tabs=4
//************************************************************
//	COPYRIGHT 2010 Moog Inc. - ALL RIGHTS RESERVED
//
// This file is the product of Moog Inc. and cannot be 
// reproduced, copied, or used in any shape or form without 
// the express written consent of Moog Inc.
//************************************************************
//
//  Defines class that manages interface to Motion Base Computer (MBC).
//  Writes commands to MBC, receives status from MBC using
//  firewire or ethernet.
//
//	Revision History: See end of file.
//
//*************************************************************




#ifndef CMBCInterfaceH
#define CMBCInterfaceH


#include "ConfigFile.h"
#include "SCCInterface.h"
#include "SignalGenerator.h"
#include <winsock2.h>


#define MAX_LEN_IP_ADDRESS      32


class CMBCInterface
{
public:
	// Enumerations

	typedef enum
	{
		eCMD_MODE_NONE = 0,
		eCMD_MODE_LENGTH,
		eCMD_MODE_DOF,
		eCMD_MODE_MDA,
		eCMD_MODE_PLAYBACK
	} TECommandMode;

	typedef enum
	{
		eDIRECT_DISPLACEMENT_MODE_NONE = 0,
		eDIRECT_DISPLACEMENT_MODE_DOF,
		eDIRECT_DISPLACEMENT_MODE_LENGTH,
	} TEDirectDisplacementMode;

	typedef enum
	{
		eWHITENOISE_MODE_NONE = 0,
		eWHITENOISE_MODE_DISPLACEMENT,
		eWHITENOISE_MODE_ACCELERATION
	} TEWhiteNoiseMode;

	typedef enum
	{
		eBUFFET_MODE_NONE = 0,
		eBUFFET_MODE_DISPLACEMENT,
		eBUFFET_MODE_ACCELERATION
	} TEBuffetMode;

	typedef enum
	{
		eDOF_AXIS_NONE = 0,
		eDOF_AXIS_ROLL,
		eDOF_AXIS_PITCH,
		eDOF_AXIS_YAW,
		eDOF_AXIS_LONGITUDINAL,
		eDOF_AXIS_LATITUDINAL,
		eDOF_AXIS_HEAVE,
	} TEDofAxis;

	typedef enum
	{
		eSIGNALGEN_AXIS_NONE = 0,
		eSIGNALGEN_AXIS_ROLL,
		eSIGNALGEN_AXIS_PITCH,
		eSIGNALGEN_AXIS_YAW,
		eSIGNALGEN_AXIS_LONGITUDINAL,
		eSIGNALGEN_AXIS_LATITUDINAL,
		eSIGNALGEN_AXIS_HEAVE,
		eSIGNALGEN_AXIS_LENGTH
	} TESignalAxis;

	typedef enum
	{
		eMDA_PARAM_NONE = 0,
		eMDA_PARAM_ROLL,
		eMDA_PARAM_PITCH,
		eMDA_PARAM_YAW,
		eMDA_PARAM_ROLLRATE,
		eMDA_PARAM_PITCHRATE,
		eMDA_PARAM_YAWRATE,
		eMDA_PARAM_ROLLACC,
		eMDA_PARAM_PITCHACC,
		eMDA_PARAM_YAWACC,
		eMDA_PARAM_LONGACC,
		eMDA_PARAM_LATACC,
		eMDA_PARAM_HEAVEACC,
		eMDA_PARAM_FREEZE,
	} TEMdaParam;

	// Public Methods

	CMBCInterface();
	~CMBCInterface();

	// Opens the interface, with pointer to configuration
	bool Open(CConfigFile* pConfig);
	// Closes the interface
	void Close();

	void SetSendThreadPriority(DWORD priority);
	void SetReceivehreadPriority(DWORD priority);

	// sends command message to interface transport
	bool SendCommandMessage();
	// loop to receive status from MBC
	void ReceiveStatusLoop();

	// Waits for MBC status state.
	bool WaitForState(ENCODED_MACHINE_STATE_TYPE eWaitMachineState, int iTimeoutMsecs, bool bExitOnFault = true);
	// Sets length position (all actuators)
	void SetLengthPosition(double dLength);
	// Sets the motion command word
	void SetMotionCommand(MOTION_CMD_WORD_TYPE iMotionCommand);
	// Sets the signal generator parameters.
	void SetSignalGenerator(TESignalAxis eSignalGenAxis,
		CSignalGenerator::TWaveForm eWaveForm,
		double dFrequency,
		double dAmplitude,
		double dOffset);
	// Sets the MBC command mode.
	void SetCommandMode(TECommandMode eMode, unsigned int uiPlaybackFileId = 0);

	// Returns true if MBC is in a fault state.
	bool InFaultState() { return ((m_iMachineState == EM_STATE_FAULT_1) || (m_iMachineState == EM_STATE_FAULT_2) || (m_iMachineState == EM_STATE_FAULT_3)); }

	// Sets the direct displacement parameters.
	void SetDirectDisplacement(TEDirectDisplacementMode eMode,
		double dVal1,
		double dVal2,
		double dVal3,
		double dVal4,
		double dVal5,
		double dVal6);

	// Sets the buffet parameters.
	void SetBuffet(TEBuffetMode eMode, unsigned int iIndex, double dFreqHz, double dAmpX, double dAmpY, double dAmpZ);
	// Sets the White Noise parameters.
	void SetWhiteNoise(TEWhiteNoiseMode eMode,
		unsigned int iIndex,
		double dAccX, double dHpX, double dLpX,
		double dAccY, double dHpY, double dLpY,
		double dAccZ, double dHpZ, double dLpZ);
	// Sets the dof axis position.
	void SetDofPosition(TEDofAxis eAxis, double dValue);

	//Getting the current DOF position.
	MBC2SCC_DOF_RESPONSE_MSG_TYPE* GetDofPosition();

	// Sets the mda parameter for mda command.
	void SetMda(TEMdaParam eMdaParam, double dValue);

	// Converts machine state string to enumerator
	ENCODED_MACHINE_STATE_TYPE GetMachineStateEnum(const char* szState);

	// Skip sending of next n cmd msgs (for testing dropped msgs)
	void SkipCommands(unsigned int uiNumCommandMsgs) { m_uiNumCommandsToSkip = uiNumCommandMsgs; }

	// Resets the command data
	void ResetCommands();

private:
	// Private Enumerations
	enum
	{
		TXRX_BUFFER_SIZE = 1024    // maximum packet size
	};

	typedef enum
	{
		eINTERFACE_ETHERNET = 0,
		eINTERFACE_FIREWIRE
	} TEInterfaceType;

	bool m_bOpen;                       // true if interface is open
	TEInterfaceType  m_eHostInterface;  // host interface type (ethernet, firewire)
	int m_iUpdateRateHz;                // MBC interface command update rate (Hz)
	bool m_bRedundantCommands;          // In length mode, if true, two msgs are sent with same position commands.

	// Ethernet Config
	char m_szMBCIpAddress[MAX_LEN_IP_ADDRESS + 1];// IP Address of MBC
	int  m_iMBCPort;                            // IP Port of MBC
	char m_szSCCIpAddress[MAX_LEN_IP_ADDRESS + 1];// IP Address of Self (SCC)
	char m_szSCCNetmask[MAX_LEN_IP_ADDRESS + 1];  // IP Net Mask of Self (SCC)
	int  m_iSCCPort;                            // IP Port of Self (SCC)

	// Interface sockets (for transmit/receive of command/status)
	SOCKET m_hostSocket;
	sockaddr_in m_destAddress;
	sockaddr_in m_localAddress;

	// Command Data (used to build command message)
	MSG_HEADER_TYPE       m_msgHeader;
	MOTION_CMD_WORD_TYPE  m_motionCommandWord;
	STAT_RESP_TYPE        m_statusResponseWord;
	SCC2MBC_LENGTH_CMD_MODE_MSG_TYPE m_lengthCmd;
	SCC2MBC_MDA_CMD_MODE_MSG_TYPE m_mdaCmd;
	SCC2MBC_SE_DOF_CMD_MSG_TYPE m_dofCmd;

	// Direct Displacement
	TEDirectDisplacementMode m_eDirectDisplamentMode;
	SCC2MBC_SE_DOF_CMD_MSG_TYPE m_DirectDisplacementDOF;
	SCC2MBC_SE_LENGTH_CMD_MSG_TYPE m_DirectDisplacementLength;

	// Buffets
	TEBuffetMode m_eBuffetMode;
	SCC2MBC_SE_BUFFET_CMD_MSG_TYPE m_BuffetCmdDis;  // Option 3
	SCC2MBC_SE_BUFFET_CMD_MSG_TYPE m_BuffetCmdAcc;  // Option 8

	// White Noise
	TEWhiteNoiseMode m_eWhiteNoiseMode;
	SCC2MBC_SE_WHITE_NOISE_CMD_MSG_TYPE m_WhiteNoiseCmdDis;        // Option 4
	SCC2MBC_SE_WHITE_NOISE_X_CMD_MSG_TYPE m_WhiteNoiseCmdAcc;      // Option 7

	char m_txStorageBuffer[TXRX_BUFFER_SIZE];  // maintains command packet (host byte order)
	char m_rxStorageBuffer[TXRX_BUFFER_SIZE];  // maintains receive packet (host byte order)
	char m_txBuffer[TXRX_BUFFER_SIZE];          // maintains command packet network byte order
	char m_rxBuffer[TXRX_BUFFER_SIZE];          // maintains receive packet network byte order

	MSG_HEADER_TYPE * m_pCommandMsg;            // pointer to command to MBC (pointer to m_txStorageBuffer)
	MSG_HEADER_TYPE * m_pStatusMsg;             // pointer to status from MBC (pointer to m_rxStorageBuffer)

	HANDLE m_hStatusChangeEvent;                 // event signaled on MBC status message change
	HANDLE m_hRcvThread;                         // handle to receive thread
	HANDLE m_hXmtThread;                         // handle to transmit thread

	ENCODED_MACHINE_STATE_TYPE m_iMachineState; // current MBC machine state
	TECommandMode m_eCommandMode;               // mode of command generation

	CSignalGenerator m_SignalGenerator;         // signal generator object
	CSignalGenerator::TWaveForm m_eSigGenWave;  // signal generator waveform 
	TESignalAxis m_eSigGenAxis;                 // axis to apply signal generator

	int m_iCmdPktLen;                           // length of command packet, used for building a command packet
	char* m_pCmdBuffer;                         // pointer to command packet buffer, used for building a command packet

	unsigned int m_uiNumCommandsToSkip;         // Number of next commands to MBC to skip, for testing dropped cmds

	//The main logger for Moog robot executions.
	std::ofstream* m_logger;

	//The current feedback position from the Moog.
	MBC2SCC_DOF_RESPONSE_MSG_TYPE *m_currentPosition;

	// Private Methods

	// Command-Packet building methods
	void CmdPacketInit();
	void CmdPacketAppend(void* pData, int iLen);
	void CmdPacketDone();
	int  CmdPacketLen();

	// builds a command message based on command settings
	int  BuildCommandMessage();
	// gets status message received from MBC
	bool GetStatusMessage();

	// swaps data between host and network order
	void SwapSCCBytes(char* pDst, char* pSrc, int iNumBytesToSwap, BYTE_ORDER_TYPE convert);

	const char *GetMotionCommandStr(MOTION_CMD_WORD_TYPE iMotionCommand);
	const char* GetMachineStateStr(int iCode);
	const char* GetMotionModeStr(int iMode);
};


#endif

//-------------------------------------------------------------------
// END OF FILE
//-------------------------------------------------------------------

