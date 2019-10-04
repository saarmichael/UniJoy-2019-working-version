// tabs=4
//************************************************************
//	COPYRIGHT 2008 Moog Inc. - ALL RIGHTS RESERVED
//
// This file is the product of Moog Inc. and cannot be 
// reproduced, copied, or used in any shape or form without 
// the express written consent of Moog Inc.
//************************************************************
//
//	$Source: /srv/cvs/EDL/TestCode/WinMotion_Base_Host/SCCInterface.h,v $
//
//	Contains motion-base host interface packet structures
//  for UDP Ethernet interface. If the motion-base ICD changes
//  then these structures must be updated to match the ICD.
//
//	Revision History: See end of file.
//
//*************************************************************


#ifndef SCCINTERFACE_H
#define SCCINTERFACE_H

class CLength
{
public:
	enum
	{
		MAX_ACTUATORS = 6
	};
};

class CDOF
{
public:
	enum
	{
		MAX_DOFS = 6
	};
};

typedef enum
{
	TO_NET_ORDER = 1,
	TO_HOST_ORDER = 2
}BYTE_ORDER_TYPE;

/*--------------------------------------------------------------------------*/
/* General Message Header Structure                                         */
/*--------------------------------------------------------------------------*/
/* Header structure */
typedef struct
{
	unsigned int  packetLength;
	unsigned int  packetSequenceCount;
	unsigned int  packetChecksum;
	unsigned int  messageID;
} MSG_HEADER_TYPE;

#define MSG_HEADER_SIZE  sizeof(MSG_HEADER_TYPE)

typedef enum
{
	/* Encoded Machine (EM) State identifiers */
	EM_STATE_UNKNOWN = -1,
	EM_STATE_POWERUP = (unsigned int)0x0, /* 0000 */
	EM_STATE_IDLE = (unsigned int)0x1, /* 0001 */
	EM_STATE_STANDBY = (unsigned int)0x2, /* 0010 */
	EM_STATE_ENGAGED = (unsigned int)0x3, /* 0011 */
	EM_STATE_SETTLING = (unsigned int)0x4, /* 0100 */
	EM_STATE_FAULT_1 = (unsigned int)0x8, /* 1000 */
	EM_STATE_FAULT_2 = (unsigned int)0x9, /* 1001 */
	EM_STATE_FAULT_3 = (unsigned int)0xA, /* 1010 */
	EM_STATE_DISABLED = (unsigned int)0xB, /* 1011 */
	EM_STATE_INHIBITED = (unsigned int)0xC, /* 1100 */
	EM_STATE_FROZEN = (unsigned int)0xD, /* 1101 */
} ENCODED_MACHINE_STATE_TYPE;


class ACTUATOR_TYPE
{
public:
	float   A;
	float   B;
	float   C;
	float   D;
	float   E;
	float   F;
};

class VECTOR_TYPE
{
public:
	float   X;
	float   Y;
	float   Z;
};

class OPTION_TYPE
{
public:
	float   optOne;
	float   optTwo;
	float   optThree;
	float   optFour;
	float   optFive;
	float   optSix;
	float   optSeven;
	float   optEight;
	float   optNine;
	float   optTen;
};

typedef enum
{
	X_AXIS = 0,
	Y_AXIS = 1,
	Z_AXIS = 2
} VECTOR_ENUM_TYPE;

// Motion Base Computer (MBC) to Source Control/Host Computer (SCC) Message ID's//
#define  MBC2SCC_STAT_RESPONSE_MSG_ID             200

#define  MAX_NUM_AXIS                               3
#define  MAX_SE_WHITE_NOISE_WAVES                   2
#define  MAX_SE_WHITE_NOISE_X_WAVES                 2
#define  MAX_SE_BUFFET_WAVES                       10
#define  MAX_OPT_FEEDBACK_DRIVES                   10

//--------------------------------------------------------------------------//
//--------------------------------------------------------------------------//
// SCC <--> MBC Command mode def'n //
// Source Control/Host Computer (SCC) To Motion Base Computer (MBC) Message ID's //
typedef enum
{
	SCC2MBC_DOF_COMMAND_MODE_MSG_ID = 100,
	SCC2MBC_LENGTH_COMMAND_MODE_MSG_ID = 101,
	SCC2MBC_MDA_COMMAND_MODE_MSG_ID = 102,
	SCC2MBC_PLAYBACK_COMMAND_MODE_MSG_ID = 103
} MOTION_COMMAND_MODE_TYPE;

//--------------------------------------------------------------------------//
//--------------------------------------------------------------------------//
// SCC <--> MBC Feedback mode def'n //
typedef enum
{
	FEEDBACK_NONE = (unsigned int)0x0,
	FEEDBACK_DATA = (unsigned int)0x1,
	FEEDBACK_ACC = (unsigned int)0x2,
	FEEDBACK_DOF = (unsigned int)0x4,
	FEEDBACK_LENGTH = (unsigned int)0x8,
} FEEDBACK_MODE_TYPE;

// MBC To SCC Motion Mode identifiers //
typedef enum
{
	MOTION_MODE_INVALID = -1,
	MOTION_MODE_MAINT = (unsigned int)0x0,
	MOTION_MODE_DOF = (unsigned int)0x1,
	MOTION_MODE_LENGTH = (unsigned int)0x2,
	MOTION_MODE_MDA = (unsigned int)0x3,
} MOTION_MODE_TYPE;

//--------------------------------------------------------------------------//
//--------------------------------------------------------------------------//
//--------------------------------------------------------------------------//
// Source Control Computer (SCC) To Motion Base Computer (MBC) Message                                        //
//--------------------------------------------------------------------------//
// SCC to MBC MotionCommandWord identifiers //
typedef enum
{
	MOTION_CMD_NULL = 0,
	MOTION_CMD_ENGAGE = 1,
	MOTION_CMD_DISENGAGE = 2,
	MOTION_CMD_RESET = 3,
	MOTION_CMD_START = 4,
	MOTION_CMD_DRAWBRIDGE_UP = 5,
	MOTION_CMD_DRAWBRIDGE_DOWN = 6,
	MOTION_CMD_DRAWBRIDGE_STOP = 7,
	MOTION_CMD_FLIGHT_COMPARTMENT_DOOR = 8, //not really a motion command word, but there was not other place for it.
} MOTION_CMD_WORD_TYPE;

typedef enum
{
	// SCC to MBC Special Effects (SE) Active Commands //
	SE_ACTIVE_CMD_DONE = 0, // Indicates that we have finished reading in all the command data  //
	SE_ACTIVE_CMD_DOF = 1, // Direct Displacement DOF Command (N/A with Message ID 101)        //
	SE_ACTIVE_CMD_LENGTH = 2, // Direct Displacement Length                                       //
	SE_ACTIVE_CMD_BUFFET = 3, // Buffet Command (Not applicable with Message ID 101)              //
	SE_ACTIVE_CMD_WHITE_NOISE = 4, // Buffet (White Noise) Command (Not applicable with Message ID 101)//
	SE_ACTIVE_CMD_VEHICLE_CG = 5, // Vehicle Center of Gravity Command		                           //
	SE_ACTIVE_CMD_VEHICLE_GROUND_SPEED = 6, // Vehicle Ground Speed				                               //
	SE_ACTIVE_CMD_WHITE_NOISE_X = 7, // Buffet (White Noise) Command With Gain Control (Not applicable with Message ID 101)
	SE_ACTIVE_CMD_BUFFET_X = 8, // Buffet Command w/ HP Filter (Not applicable with Message ID 101)              //
} SE_ACTIVE_TYPE;

// Structure that defines all of the SE. active message types //
class SE_MSG_ACTIVE_TYPE
{
public:
	char dOF;
	char length;
	char buffet;
	char whiteNoise;
	char vehicleCG;
};

// One 32bit word - Status Response Info //
typedef union
{
	unsigned int      theValue;
	struct
	{
		unsigned int      NumOptFeedbackParameters : 4; //1-10
		unsigned int      RequestedFeedbackMode : 4;
		unsigned int      ReplayFile : 8;
		unsigned int      updateRate : 16;
	} theBits;
} STAT_RESP_TYPE;

// DOF Command message type //
class SCC2MBC_DOF_CMD_MODE_MSG_TYPE
{
public:
	float                 roll;
	float                 pitch;
	float                 yaw;
	float                 longitudinal;
	float                 lateral;
	float                 heave;
};

#define SIZEOF_DOF_CMD_MODE_MSG  sizeof(SCC2MBC_DOF_CMD_MODE_MSG_TYPE)

// Length command message type //
class SCC2MBC_LENGTH_CMD_MODE_MSG_TYPE
{
public:
	ACTUATOR_TYPE          actuatorLength;
};
#define SIZEOF_LENGTH_CMD_MODE_MSG  sizeof(SCC2MBC_LENGTH_CMD_MODE_MSG_TYPE)

// MDA command message type //
// BDF - Added mdaCommandWord to match NLX definition of MDA Msg in mtn_local.h
class SCC2MBC_MDA_CMD_MODE_MSG_TYPE
{
public:
	unsigned int          mdaCommandWord;
	float                 roll;
	float                 pitch;
	float                 yaw;
	float                 rollRate;
	float                 pitchRate;
	float                 yawRate;
	float                 rollAcceleration;
	float                 pitchAcceleration;
	float                 yawAcceleration;
	float                 longitudinalAcceleration;
	float                 lateralAcceleration;
	float                 verticalAcceleration;
};
#define SIZEOF_MDA_CMD_MODE_MSG  sizeof(SCC2MBC_MDA_CMD_MODE_MSG_TYPE)

// Playback command message type //
class SCC2MBC_PLAYBACK_CMD_MODE_MSG_TYPE
{
public:
};
#define SIZEOF_PLAYBACK_CMD_MODE_MSG  sizeof(SCC2MBC_PLAYBACK_CMD_MODE_MSG_TYPE)

//--------------------------------------//
// Special Effects Commands (se)        //
//--------------------------------------//
// Option = 1                   //
class SCC2MBC_SE_DOF_CMD_MSG_TYPE
{
public:
	float              roll;
	float              pitch;
	float              yaw;
	float              longitudinal;
	float              lateral;
	float              heave;
};
#define SIZEOF_SE_DOF_CMD_MSG  (sizeof(SCC2MBC_SE_DOF_CMD_MSG_TYPE))

// Option = 2 Direct Displacement                   //
class SCC2MBC_SE_LENGTH_CMD_MSG_TYPE
{
public:
	ACTUATOR_TYPE      actuatorLength;
};
#define SIZEOF_SE_LENGTH_CMD_MSG  (sizeof(SCC2MBC_SE_LENGTH_CMD_MSG_TYPE))

// Option = 3   Buffet     //
class SCC2MBC_SE_BUFFET_CMD
{
public:
	float	frequency;
	float	amplitude[MAX_NUM_AXIS];
};

class SCC2MBC_SE_BUFFET_CMD_MSG_TYPE
{
public:
	unsigned int	numSineWaves; // 1-10
	SCC2MBC_SE_BUFFET_CMD	Buffet[MAX_SE_BUFFET_WAVES];
};
#define SIZEOF_SE_BUFFET_CMD_MSG  (sizeof(SCC2MBC_SE_BUFFET_CMD_MSG_TYPE))

// Option = 4       White Noise //
class SCC2MBC_SE_WHITE_NOISE_CMD
{
public:
	float	amplitude[MAX_NUM_AXIS];
};

class SCC2MBC_SE_WHITE_NOISE_CMD_MSG_TYPE
{
public:
	unsigned int       numWhiteNoiseWaves; // 1-2
	SCC2MBC_SE_WHITE_NOISE_CMD WhiteNoise[MAX_SE_WHITE_NOISE_WAVES];
};
#define SIZEOF_SE_WHITE_NOISE_CMD_MSG  (sizeof(SCC2MBC_SE_WHITE_NOISE_CMD_MSG_TYPE))

// Option = 5   Vehicle CG    //
class SCC2MBC_SE_VEHICLE_CG_CMD_MSG_TYPE
{
public:
	VECTOR_TYPE        vehicleCG;
};
#define SIZEOF_SE_VEHICLE_CG_CMD_MSG  (sizeof(SCC2MBC_SE_VEHICLE_CG_CMD_MSG_TYPE))

// Option = 6   Vehicle GROUND_SPEED    //
class SCC2MBC_SE_VEHICLE_GROUND_SPEED_CMD_MSG_TYPE
{
public:
	unsigned int    vehicleOnGround;
	float  vehicleGroundSpeed;
};

#define SIZEOF_SE_VEHICLE_GROUND_SPEED_CMD_MSG  (sizeof(SCC2MBC_SE_VEHICLE_GROUND_SPEED_CMD_MSG_TYPE))

// Option = 7       White Noise With Gain Control//
class SCC2MBC_SE_WHITE_NOISE_X_CMD
{
public:
	float	amplitude;
	float	HP_Roll_Off;
	float	LP_Roll_Off;
};

class SCC2MBC_SE_WHITE_NOISE_X_CMD_MSG_TYPE
{
public:
	unsigned int					numWhiteNoiseXWaves; // 1-2
	SCC2MBC_SE_WHITE_NOISE_X_CMD	WhiteNoiseX[MAX_SE_WHITE_NOISE_X_WAVES*MAX_NUM_AXIS];
};
#define SIZEOF_SE_WHITE_NOISE_X_CMD_MSG  (sizeof(SCC2MBC_SE_WHITE_NOISE_X_CMD_MSG_TYPE))


//--------------------------------------------------------------------------//
//--------------------------------------------------------------------------//
//--------------------------------------------------------------------------//
// 2) Motion Base Computer (MBC) To Source Control Computer (SCC)           //
//--------------------------------------------------------------------------//

// MBC To SCC Optional (OPT) Status Data //
typedef enum
{
	OPT_STATUS_DATA_DONE = 0, // Indicates that we have finished reading in all the command data  //
	OPT_STATUS_DATA_DOF = 1, // Direct Displacement DOF Command (N/A with Message ID 101)        //
	OPT_STATUS_DATA_LENGTH = 2, // Direct Displacement Length                                       //
	OPT_STATUS_DATA = 3, // Data                                              //
	OPT_STATUS_DATA_PLATFORM_ACC = 4,	// Platform Acc
} OPT_STAT_DATA_TYPE;

// Structure that defines all of the Optional (OPT) Status Data types //
class OPT_MSG_STAT_TYPE
{
public:
	char DOF;
	char length;
	char data;
	char platformAcc;
};

// Machine Response Type //
typedef union
{
	unsigned int      theValue;
	struct
	{
		unsigned int       encodedMachineState : 4;
		unsigned int       motionMode : 2;
		unsigned int       feedbackMode : 2;
		unsigned int       MiscellaneousOptions : 8; // 00-FF //
		unsigned int       updateRate : 16;
	} theBits;
} MACHINE_RESPONSE_TYPE;

//////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////

class MBCSCC_TXRX_DATA_TYPE
{
public:
	MOTION_CMD_WORD_TYPE motionCommandWord;
	FEEDBACK_MODE_TYPE   RequestedFeedbackMode;
	unsigned int         numOptFeedbackParameters;
	unsigned int         MiscellaneousOptions;
	unsigned int         updateRate;
	unsigned int         numOptFeedbackDrives;
};

// Option 1 = DOF Status Data //
class MBC2SCC_DOF_RESPONSE_MSG_TYPE
{
public:
	float                  rollFeedback;
	float                  pitchFeedback;
	float                  yawFeedback;
	float                  longitudinalFeedback;
	float                  lateralFeedback;
	float                  heaveFeedback;
	OPT_STAT_DATA_TYPE     optStatusData;
};
#define SIZEOF_DOF_RESPONSE_MSG  (sizeof(MBC2SCC_DOF_RESPONSE_MSG_TYPE))

// Option 2 = Length Status Data //
class MBC2SCC_LENGTH_RESPONSE_MSG_TYPE
{
public:
	ACTUATOR_TYPE          actuatorLengthFeedback;
	OPT_STAT_DATA_TYPE     optStatusData;
};
#define SIZEOF_LENGTH_RESPONSE_MSG  (sizeof(MBC2SCC_LENGTH_RESPONSE_MSG_TYPE))

// Option 3 = Optional Drive Status Data //
class MBC2SCC_DATA_RESPONSE_MSG_TYPE
{
public:
	unsigned int         numOptFeedbackDrives; // 1 - 10 //
	OPTION_TYPE        	optDriveFeedback[CLength::MAX_ACTUATORS];
	OPT_STAT_DATA_TYPE   optStatusData;
};
#define SIZEOF_DATA_RESPONSE_MSG  (sizeof(MBC2SCC_DATA_RESPONSE_MSG_TYPE))

// Option = 4 = Accelerometer
class MBC2SCC_ACC_RESPONSE_MSG_TYPE
{
public:
	VECTOR_TYPE          platformAcc;
	OPT_STAT_DATA_TYPE   optStatusData;
};
#define SIZEOF_ACC_RESPONSE_MSG  (sizeof(MBC2SCC_ACC_RESPONSE_MSG_TYPE))




// Discrete IO Values //
typedef union
{
	unsigned int      theValue;
	struct
	{
		unsigned int       maintMode : 1;					//0
		unsigned int       baseAtHome : 1;				//1
		unsigned int       EStopInputActive : 1;			//2
		unsigned int       startInputActive : 1;			//3
		unsigned int       stopInputActive : 1;			//4
		unsigned int       DrawBridge_Safe : 1;			//5
		unsigned int       DrawBridge_Down : 1;			//6
		unsigned int       DrawBridgeGateSafetySwitch : 1;//7
		unsigned int       DrawBridgeMat : 1;				//8
		unsigned int       CabinetResetButton : 1;		//9
		unsigned int       DrawBridge_SafeTwo : 1;		//10
		unsigned int       unusedBits : 21;				//11
	} theBits;
} DISCRETE_IO_TYPE;

// Latched Fault Data Word #1//
typedef union
{
	unsigned int      theValue;
	struct
	{
		unsigned int       hostComm : 1; //0
		unsigned int       upsBatteryLow : 1;//1
		unsigned int       DrawBridgeNotUpFlag : 1; //2
		unsigned int       DrawBridgeLimitSwitchFlag : 1; //3
		unsigned int       DrawBridgeGateSafetyFlag : 1; //4
		unsigned int       DrawBridgeUpRequestTimeoutFlag : 1; //5
		unsigned int       DrawBridgeDownRequestTimeoutFlag : 1; //6
		unsigned int       snubberPressure : 1; //7
		unsigned int       DrawBridgeConnectFlag : 1; //8
		unsigned int       unusedBits1 : 1; //9
		unsigned int       SystemInterlock1 : 1; //10
		unsigned int       SystemInterlock2 : 1; //11
		unsigned int       SystemInterlock3 : 1; //12
		unsigned int       SystemInterlock4 : 1; //13
		unsigned int       SystemInterlock5 : 1; //14
		unsigned int       SystemInterlock6 : 1; //15
		unsigned int       SystemInterlock7 : 1; //16
		unsigned int       SystemInterlock8 : 1; //17
		unsigned int       SystemInterlock9 : 1; //18
		unsigned int       SystemInterlock10 : 1; //19
		unsigned int       SystemInterlock11 : 1; //20
		unsigned int       SystemInterlock12 : 1; //21
		unsigned int       DrawBridgeBatteryLowFlag : 1; //22
		unsigned int       RegenResistoOverTemp : 1;//23
		unsigned int	   DrawBridgeSafetyModFlag : 1;//24
		unsigned int	   AtRestFlag : 1; //25
		unsigned int	   unusedBits2 : 1; //26 
		unsigned int	   InterlockConflict : 1; //27
		unsigned int       rthBatteryLow : 1; //28
		unsigned int       unusedBits4 : 1; //29
		unsigned int       DCBusDissipation : 1; //30
		unsigned int       rth : 1; //31
	} theBits;
} LF_DW_1_TYPE;

// Latched Fault Data Word #2//
typedef union
{
	unsigned int      theValue;
	struct
	{
		unsigned int		acPower : 1;//0 	
		unsigned int		plcComm : 1; //1
		unsigned int		controllerFault : 1; //2
		unsigned int		CabinetMotionDisableSwitch : 1; //3
		unsigned int		FollowingError : 1; //4
		unsigned int		HitHomeWhileMoving : 1; //5
		unsigned int		HostCommandRangeExceeded : 1; //6
		unsigned int		VDC300PowerSupplyFault : 1; //7 
		unsigned int		pcToPLCInterlock : 1; //8 
		unsigned int		plcToPCInterlock : 1; //9
		unsigned int		plcToIFBInterlock : 1; //10
		unsigned int		unusedBits3 : 1; //11
		unsigned int		IFBtoPLCInterlock : 1; //12
		unsigned int		DrawBridgeLoopMonitor : 1; //13-15
		unsigned int		unusedBits4 : 2; //13-15
		unsigned int		EStop : 1; // 16
		unsigned int		DrawBridgeSM1Mon : 1; // 17 - 23
		unsigned int		DrawBridgeSM2Mon : 1; // 17 - 23
		unsigned int		DrawBridgeSM3Mon : 1; // 17 - 23
		unsigned int		unusedBits5 : 4; // 17 - 23
		unsigned int		DriveCommunication : 1; // 24
		unsigned int		OverallDriveFault : 1; // 25
		unsigned int		DSPDriveFault : 1; // 26
		unsigned int		MCCDriveFault : 1; // 27
		unsigned int		DBENFault : 1; // 28
		unsigned int		unusedBits6 : 3; // 29 - 31
	}theBits;
} LF_DW_2_TYPE;

// Latched Fault Data Word #3//
typedef union
{
	unsigned int      theValue;
	struct
	{
		unsigned int       AccelerationFault : 1; //0
		unsigned int       VelocityFault : 1; //1
		unsigned int       unusedBits0 : 1; //2
		unsigned int       EnvelopeExtendLimit : 1; //3
		unsigned int       EnvelopeRetractLimit : 1; //4
		unsigned int       unusedBits1 : 27; //5-31

	}theBits;
} LF_DW_3_TYPE;

////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////
//*	
// MB Status Response Message (201) //
class MBC2SCC_MB_STAT_RESPONSE_MSG_TYPE
{
public:
	MSG_HEADER_TYPE         msgHeader;
	MACHINE_RESPONSE_TYPE   statusResponseWord;
	DISCRETE_IO_TYPE    	discreteIO;
	LF_DW_1_TYPE        	latchedFault1;
	LF_DW_2_TYPE        	latchedFault2;
	LF_DW_3_TYPE        	latchedFault3;
	OPT_STAT_DATA_TYPE      	optStatusData;
};
#define SIZEOF_MB_STAT_RESPONSE_MSG  (sizeof(MBC2SCC_MB_STAT_RESPONSE_MSG_TYPE) - MSG_HEADER_SIZE)



#endif // SCCINTERFACE_H

//---------------------------------------------------------------
// END OF FILE
//---------------------------------------------------------------
