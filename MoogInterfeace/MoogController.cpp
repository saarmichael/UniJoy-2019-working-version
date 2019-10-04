#include "MoogController.h"

MoogController::MoogController(char initFilePath[], ofstream * logFile)
{
	m_mbcInterface = new CMBCInterface();

	m_config = new CConfigFile(initFilePath, logFile);

	m_logFile = logFile;

	m_scriptFile = new CScriptFile(m_mbcInterface, m_logFile);
}

void MoogController::Connect()
{
	m_mbcInterface->Open(m_config);
}

void MoogController::Engage()
{
	m_scriptFile->Engage();
}

void MoogController::Disengage()
{
	m_scriptFile->Disengage();
}

void MoogController::SendPositions(MoogFrame* position)
{
	m_scriptFile->SendMBCAxesPositions(position);
}

void MoogController::SendPosition(double surge, double heave, double lateral, double yaw, double roll, double pitch)
{
	MoogFrame* position = new MoogFrame();
	position->heave = heave;
	position->surge = surge;
	position->lateral = lateral;
	position->pitch = pitch;
	position->yaw = yaw;
	position->roll = roll;

	m_scriptFile->SendMBCAxesPositions(position);
}

void MoogController::Disconnect()
{
	m_mbcInterface->Close();
}
