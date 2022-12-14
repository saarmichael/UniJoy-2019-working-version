using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Trajectories;
using Trajectories.TrajectoryCreators;
using Params;
using MoveController;
using WMPLib;
using log4net;
using System.Windows.Forms.DataVisualization.Charting;
using System.Threading;
using MathNet.Numerics.Distributions;
using UnityVR;
using Newtonsoft.Json;

namespace UniJoy
{
    /// <summary>
    /// This class is the main program controll loop.
    /// It calls all the needed other inerfaces to make what's needed to be created.
    /// The function is called by the GuiInterface after the statButton is clicked.
    /// </summary>
    public class ControlLoop : IDisposable
    {
        const bool UPDATE_GLOBAL_DETAILS_LIST_VIEW = false;

        #region CONSTANTS
        /// <summary>
        /// Indicates if when moving the forward and backward trajectories need to wait the job end by the bsc yasakawa i/o indicator or by the given force timing parameter.
        /// </summary>
        bool YASKAWA_TRAJECTORY_MOVEMENTS_JOB_WAIT_BY_IO = false;
        #endregion CONSTANTS

        #region ATTRIBUTES
        /// <summary>
        /// The trajectory creator interface for making the trajectory for each trial.
        /// </summary>
        private ITrajectoryCreator _trajectoryCreator;

        /// <summary>
        /// The trajectory creation 
        /// </summary>
        private TrajectoryCreatorHandler _trajectoryCreatorHandler;

        /// <summary>
        /// The variables readen from the xlsx protocol file.
        /// </summary>
        private Variables _variablesList;

        /// <summary>
        /// Final list holds all the current cross varying vals by dictionary of variables with values for each line(trial) for both ratHouseParameters.
        /// </summary>
        private List<Dictionary<string, double>> _crossVaryingVals;

        /// <summary>
        /// The static variables list in double value presentation.
        /// The string is for the variable name.
        /// </summary>
        private Dictionary<string, double> _staticVariablesList;

        /// <summary>
        /// The numbers of samples for each trajectory.
        /// </summary>
        private int _frequency;

        /// <summary>
        /// The name of the selected protocol.
        /// </summary>
        public string ProtocolFullName;

        /// <summary>
        /// The current varying trial combination that should be selected to make the trajectory from.
        /// </summary>
        private int _currentVaryingTrialIndex;

        /// <summary>
        /// The total number of trials for the experiment should have.
        /// </summary>
        private int _totalNumOfTrials;

        /// <summary>
        /// The total number of correct answers(head stability during the trial movement plus correct decision).
        /// </summary>
        private int _totalCorrectAnswers;

        /// <summary>
        /// Thr total number of enterance to the center during timeout time with stability during the duration time.
        /// </summary>
        private int _totalHeadStabilityInCenterDuringDurationTime;

        /// <summary>
        /// The total success states for a trial (correct or wrong answer but some answer).
        /// </summary>
        private int _totalChoices;

        /// <summary>
        ///The total number of head fixation breaks during the duration time.
        /// </summary>
        private int _totalHeadFixationBreaks;

        /// <summary>
        /// The total number of head fixation breaks only during the start delay time.
        /// </summary>
        private int _totalHeadFixationBreaksStartDelay;

        /// <summary>
        /// The varying index selector for choosing the current combination index.
        /// </summary>
        private VaryingIndexSelector _varyingIndexSelector;

        /// <summary>
        /// The current repetition index number.
        /// </summary>
        private int _repetitionIndex;

        /// <summary>
        /// The current stickOn index number.
        /// </summary>
        private int _stickOnNumberIndex;

        /// <summary>
        /// Includes all the currebt trial timings and delays.
        /// </summary>
        private TrialTimings _currentTrialTimings;

        /// <summary>
        /// The current trial trajectories.
        /// The first element in the tuple is the ratHouseTrajectory.
        /// The second element in the tuple is the landscapeHouseTrajectory.
        /// </summary>
        private Tuple<Trajectory, Trajectory> _currentTrialTrajectories;

        /// <summary>
        /// The current trial stimulus type.
        /// </summary>
        private int _currentTrialStimulusType;

        /// <summary>
        /// A random object for random numbers.
        /// </summary>
        private Random _timingRandomizer;

        //TODO: do I need this?
        /// <summary>
        /// The robot reward controller.
        /// </summary>
        //private RewardController _rewardController;

        //TODO: do I need this?
        /// <summary>
        /// Controller for the rat Noldus responses.
        /// </summary>
        //private RatResponseController _ratResponseController;

        //TODO: do I need this?
        /// <summary>
        /// Controller for writing events for the AlphaOmega.
        /// </summary>
        //private AlphaOmegaEventsWriter _alphaOmegaEventsWriter;

        //TODO: do I need this?
        /// <summary>
        /// Infra red controller for turnnig the InfraRed on/off.
        /// </summary>
        //private InfraRedController _infraredController;

        /// <summary>
        /// Indicated if the control loop should not make another trials.
        /// </summary>
        private bool _stopAfterTheEndOfTheCurrentTrial;

        /// <summary>
        /// Indicated if the control loop should paused until resume button is pressed.
        /// </summary>
        private bool _pauseAfterTheEndOfTheCurrentTrial;

        /// <summary>
        /// Describes the delegate for a control with it's nick name.
        /// </summary>
        private Dictionary<string, Delegate> _mainGuiControlsDelegatesDictionary;

        /// <summary>
        /// Describes the control object with it's nick name.
        /// </summary>
        private Dictionary<string, Control> _mainGuiInterfaceControlsDictionary;

        /// <summary>
        /// Timer initialize each trial start and reset onlt at he end of the trial.
        /// </summary>
        private Stopwatch _controlLoopTrialTimer;

        /// <summary>
        /// A stopwatch for total time of handreward time counter.
        /// </summary>
        private Stopwatch _handRewardTotalTimer;

        /// <summary>
        /// A dictionary include a key for the ecvent name and a double for the time of the event since the start of the trial. Each trial the dictionary cleared.
        /// </summary>
        private Dictionary<string, double> _trialEventRealTiming;

        /// <summary>
        /// The current rat sampling response come from the Noldus.
        /// The sampling rate is readen from solution settings configuration.
        /// </summary>
        private byte _currentRatResponse;

        /// <summary>
        /// The rat decision about the current trial stimulus direction.
        /// </summary>
        private RatDecison _currentRatDecision;

        /// <summary>
        /// Indicates if the rat decision should be inverse to the heading due to the random heading direction region.
        /// </summary>
        private bool _inverseRRDecision;

        /// <summary>
        /// The decision the rat should choose.
        /// </summary>
        private RatDecison _correctDecision;

        /// <summary>
        /// The selected rat name (that makes the experiment).
        /// </summary>
        public string RatName { get; set; }

        /// <summary>
        /// The name of the student that makes the experiment.
        /// </summary>
        public string StudentName { get; set; }

        /// <summary>
        /// The number of repetitions for the varying set.
        /// </summary>
        public int NumOfRepetitions { get; set; }

        /// <summary>
        /// The number of stick on number for each specific value in the optional values for rounds (must devide NumOfRepetitions).
        /// </summary>
        public int NumOfStickOn { get; set; }

        /// <summary>
        /// The percentage number of turned on LEDS.
        /// </summary>
        public double PercentageOfTurnedOnLeds { get; set; }

        /// <summary>
        /// The LEDs brightness value (0-31).
        /// </summary>
        public int LEDBrightness { get; set; }

        /// <summary>
        /// The red color value for the leds.
        /// </summary>
        public int LEDcolorRed { get; set; }

        /// <summary>
        /// The green color value for the leds.
        /// </summary>
        public int LEDcolorGreen { get; set; }

        /// <summary>
        /// The blue color value for the leds.
        /// </summary>
        public int LEDcolorBlue { get; set; }

        /// <summary>
        /// Timer for raising event to sample the Noldus reponse direction and store it in _currentRatResponse.
        /// </summary>
        private System.Timers.Timer _ratSampleResponseTimer;

        /// <summary>
        /// Timer for raising event for counting the water the rat have rewarded so far.
        /// </summary>
        private System.Timers.Timer _waterRewardFillingTimer;

        /// <summary>
        /// The SavedDataMaker object to create new result file for each experiment.
        /// </summary>
        private SavedDataMaker _savedExperimentDataMaker;

        /// <summary>
        /// Logger for writing log information.
        /// </summary>
        private ILog _logger;

        /// <summary>
        /// The online psyco graph maker object to control the psycho chart.
        /// </summary>
        private OnlinePsychGraphMaker _onlinePsychGraphMaker;

        /// <summary>
        /// The task executing the robot forward motion untill finished.
        /// </summary>
        private Task _robotMotionTask;

        /// <summary>
        /// Indictaed wether giving a second reward automatically in the direction side of the stimulus type.
        /// </summary>
        public bool AutoReward { get; set; }

        /// <summary>
        /// Indicated if whether to wait for a rat to enter it's head to the center automatically skiping this step.
        /// </summary>
        public bool AutoStart { get; set; }

        /// <summary>
        /// Indicated if to check the fixation during the duration time or to skip that stage.k
        /// </summary>
        public bool AutoFixation { get; set; }

        /// <summary>
        /// Indicated if to sound a media during the reward with the same direction.
        /// </summary>
        public bool CenterRewardSound { get; set; }

        public bool SideRewardSound { get; set; }

        /// <summary>
        /// Indicates if the mode of the trial is only untill the fixation stage (include).
        /// </summary>
        public bool FixationOnlyMode { get; set; }

        /// <summary>
        /// Indicates if to enable fixation break sound.
        /// </summary>
        public bool EnableFixationBreakSound { get; set; }

        /// <summary>
        /// Indicates whethear to enable error sound for a wrong choice.
        /// </summary>
        public bool EnableErrorSound { get; set; }

        /// <summary>
        /// Indicates if to enable clue sound in both sides.
        /// </summary>
        public bool EnableCueSoundInBothSide { get; set; }

        /// <summary>
        /// Indicates if to enable clue sound only in the correct side.
        /// </summary>
        public bool EnableCueSoundCorrectSide { get; set; }

        /// <summary>
        /// Indicates if to enable clue sound (can be one of the both sided or correct side option).
        /// </summary>
        public bool EnableGoCueSound { get; set; }

        /// <summary>
        /// Indicates if to enable random correct answer in delta's protocols while the stimulus direction's are inversed in the sign.
        /// </summary>
        public bool EnableRRDelta { get; set; }

        /// <summary>
        /// Indicates if to enable that right parameters values and left parameters values must be equals.
        /// </summary>
        public bool EnableRightLeftMustEquals { get; set; }

        /// <summary>
        /// Indicates the autos options that are commanded in the real time (when the code use it at the conditions and not only if the user change it betweens).
        /// </summary>
        public AutosOptions _autosOptionsInRealTime { get; set; }

        /// <summary>
        /// Indicates the special modes that are commanded in the real time.
        /// </summary>
        public SpecialModes _specialModesInRealTime { get; set; }

        /// <summary>
        /// Indicates the sounds modes that are commanded in the real time.
        /// </summary>
        public SoundsMode _soundsMode { get; set; }

        /// <summary>
        /// Indicated if to give the rat a second response chance if it wrong anser at the first time (but not include no answer).
        /// </summary>
        public bool SecondResponseChance { get; set; }

        /// <summary>
        /// Dictionary represent a sound name and it's file path.
        /// </summary>
        private Dictionary<string, string> _soundPlayerPathDB;

        /// <summary>
        /// Object for handles WindowsMediaPlayer controls to play mp3 files.
        /// </summary>
        private WindowsMediaPlayer _windowsMediaPlayer;

        //Maayan Edit
        private RemoteController _remoteController;

        #endregion ATTRIBUTES

        #region CONTRUCTORS
        /// <summary>
        /// Default constructor.
        /// <param name="matlabApp">The matlab app handler.</param>
        /// <param name="motomanController">The motoman controller object.</param>
        /// <param name="ledController">The led controller object.</param>
        /// <param name="infraRedController">The InfraRed controller for turning on/off the InfraRed.</param>
        /// <param name="ctrlDelegatesDic">The controls delegate names and objects.</param>
        /// <param name="mainGuiInterfaceControlsDictionary">The name of each main gui needed control and it's reference.</param>
        /// <param name="logger">The program logger for logging into log file.</param>
        /// </summary>
        public ControlLoop(Dictionary<string, Delegate> ctrlDelegatesDic,
            Dictionary<string, Control> mainGuiInterfaceControlsDictionary, ILog logger)
        {
            _trajectoryCreatorHandler = new TrajectoryCreatorHandler();

            //copy the logger reference to writing lof information
            _logger = logger;

            //TODO: Do I need this?
            /*_rewardController = new RewardController("Dev1", "Port1", "Line0:2", "RewardChannels");
            _ratResponseController = new RatResponseController("Dev1", "Port0", "Line0:2", "RatResponseChannels");
            _alphaOmegaEventsWriter = new AlphaOmegaEventsWriter("Dev1", "Port0", "Line3:7", "AlphaOmegaEventsChannels", "Port1", "Line3", "AlphaOmegaStrobeChannel", _logger);
            _infraredController = infraRedController;*/

            _stopAfterTheEndOfTheCurrentTrial = false;

            //configure  rge timer for the sampling Noldus rat response direction.
            _ratSampleResponseTimer = new System.Timers.Timer(Properties.Settings.Default.NoldusRatReponseSampleRate);
            //_ratSampleResponseTimer.Elapsed += SetRatReponse;

            //configure the water filling timer for the water reward estimation interactive window.
            _waterRewardFillingTimer = new System.Timers.Timer();
            _waterRewardFillingTimer.Interval = 100;
            _waterRewardFillingTimer.Elapsed += WaterRewardFillingTimer_Tick;

            //TODO: Do I need this?
            //take the motoman controller object.
            //_motomanController = motomanController;

            //TODO: Do I need this?
            //take the led controller object.
            //_ledControllerRight = ledController;
            //_ledControllerLeft = ledController2;
            //initialize the leds index selector.
            //_ledSelectorRight = new LedsSelector(150, 10);
            //_ledSelectorLeft = new LedsSelector(150, 10);

            //init the trial events details.
            _trialEventRealTiming = new Dictionary<string, double>();
            _controlLoopTrialTimer = new Stopwatch();

            //init the hand reward stopwatch time account.
            _handRewardTotalTimer = new Stopwatch();

            //initialize the savedDataMaker object once.
            _savedExperimentDataMaker = new SavedDataMaker();

            //initialize the controls changed by the controlLoop and their invoking functions.
            _mainGuiControlsDelegatesDictionary = ctrlDelegatesDic;
            _mainGuiInterfaceControlsDictionary = mainGuiInterfaceControlsDictionary;

            //initialize the online psycho graph.
            _onlinePsychGraphMaker = new OnlinePsychGraphMaker
            {
                ClearDelegate = _mainGuiControlsDelegatesDictionary["OnlinePsychoGraphClear"],
                SetSeriesDelegate = _mainGuiControlsDelegatesDictionary["OnlinePsychoGraphSetSeries"],
                SetPointDelegate = _mainGuiControlsDelegatesDictionary["OnlinePsychoGraphSetPoint"],
                ChartControl = _mainGuiInterfaceControlsDictionary["OnlinePsychoGraph"] as Chart
            };

            //initialize the MrdiaPlayer controller and the DB of sound effects and their path.
            _soundPlayerPathDB = new Dictionary<string, string>();
            LoadAllSoundPlayers();
            _windowsMediaPlayer = new WindowsMediaPlayer();

            //Maayan edit
            _remoteController = new RemoteController(_logger);
        }

        /// <summary>
        /// Clear all the control loop items and timers.
        /// </summary>
        public void Dispose()
        {
            //stop the timer for sampling the rat Noldus response.
            _ratSampleResponseTimer.Stop();

            //remove the timer for rat Noldus response.
            //_ratSampleResponseTimer.Elapsed -= SetRatReponse;
        }
        #endregion CONTRUCTORS

        #region FUNCTIONS

        #region GUI_COMMANDS
        /// <summary>
        /// Transfer the control from the main gui to the control loop until a new gui event is handled by the user.
        /// </summary>
        public void Start(Variables variablesList, List<Dictionary<string, double>> crossVaryingList, Dictionary<string, double> staticVariablesList, int frequency, ITrajectoryCreator trajectoryCreatorName)
        {
            //initialize variables.
            _variablesList = variablesList;
            _crossVaryingVals = crossVaryingList;
            _staticVariablesList = staticVariablesList;
            _frequency = frequency;

            //initialize counters and varying selector.
            _totalNumOfTrials = _crossVaryingVals.Count();
            _varyingIndexSelector = new VaryingIndexSelector(_totalNumOfTrials);
            _totalCorrectAnswers = 0;
            _totalHeadStabilityInCenterDuringDurationTime = 0;
            _totalChoices = 0;
            _totalHeadFixationBreaks = 0;
            _totalHeadFixationBreaksStartDelay = 0;

            //reset repetition index and stickOnNumber (would be reset in the loop immediately because the condition == NumOfStickOn).
            _repetitionIndex = 0;
            _stickOnNumberIndex = NumOfStickOn;

            _timingRandomizer = new Random();

            //reset the experiement total reward stopwatch accounting.
            _handRewardTotalTimer.Reset();

            //set the trajectory creator name to the given one that should be called in the trajectoryCreatorHandler.
            //also , set the other properties.
            _trajectoryCreatorHandler.SetTrajectoryAttributes(trajectoryCreatorName, _variablesList, _crossVaryingVals, _staticVariablesList, _frequency);

            //TODO: Do I need this?
            //reset the RewardController outputs.
            //_rewardController.ResetControllerOutputs();

            //TODO: Do I need this?
            //set the frequency for the JBI file creator.
            //_motomanController.MotomanProtocolFileCreator.Frequency = _frequency;

            //create a new results file for the new experiment.
            _savedExperimentDataMaker.CreateControlNewFile(ProtocolFullName);

            //clear and initialize the psyco online graph.
            _onlinePsychGraphMaker.Clear();
            _onlinePsychGraphMaker.VaryingParametrsNames = GetVaryingVariablesList();
            _onlinePsychGraphMaker.HeadingDireactionRegion = new Region
            {
                LowBound = double.Parse(_variablesList._variablesDictionary["HEADING_DIRECTION"]._description["low_bound"]._MoogParameter),
                Increament = double.Parse(_variablesList._variablesDictionary["HEADING_DIRECTION"]._description["increament"]._MoogParameter),
                HighBound = double.Parse(_variablesList._variablesDictionary["HEADING_DIRECTION"]._description["high_bound"]._MoogParameter)
            };
            _onlinePsychGraphMaker.InitSerieses();

#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            //reset the amount of water measurement interactive panel.
            _mainGuiInterfaceControlsDictionary["SetWaterRewardsMeasure"].BeginInvoke(
              _mainGuiControlsDelegatesDictionary["SetWaterRewardsMeasure"], true);
#endif

            //run the main control loop function in other thread from the main thread ( that handling events and etc).
            _stopAfterTheEndOfTheCurrentTrial = false;
            Globals._systemState = SystemState.RUNNING;
            _ratSampleResponseTimer.Start();
            Task.Run(() => MainControlLoop());
        }

        /// <summary>
        /// Stop the MainControlLoop function.
        /// </summary>
        public void Stop()
        {
            //Globals._systemState = SystemState.STOPPED;
            _stopAfterTheEndOfTheCurrentTrial = true;
        }

        /// <summary>
        /// Pause the MainControlLoop function.
        /// </summary>
        public void Pause()
        {
            _pauseAfterTheEndOfTheCurrentTrial = true;
        }

        /// <summary>
        /// Resume the MainControlLoop.
        /// </summary>
        public void Resume()
        {
            _pauseAfterTheEndOfTheCurrentTrial = false;

            Task.Run(() => MainControlLoop());
        }
        #endregion

        #region STAGES_FUNCTION
        public void MainControlLoop()
        {
            _logger.Info("Main ControlLoop begin.");

            for (; _repetitionIndex < NumOfRepetitions / NumOfStickOn;)
            {
                //while all trial are not executed or not come with response stage.
                while (!_varyingIndexSelector.IsFinished())
                {
                    //if system has stopped , wait for the end of the current trial ans break,
                    if (Globals._systemState.Equals(SystemState.STOPPED) || _stopAfterTheEndOfTheCurrentTrial || _pauseAfterTheEndOfTheCurrentTrial)
                    {
                        Globals._systemState = SystemState.STOPPED;

                        if (!_pauseAfterTheEndOfTheCurrentTrial)
                            EndControlLoopByStopFunction();

                        return;
                    }

                    //initialize the stickOnNumber if needed and choose a new varyingIndex.
                    if (_stickOnNumberIndex == NumOfStickOn)
                    {
                        //choose the random combination index for the current trial.
                        _currentVaryingTrialIndex = _varyingIndexSelector.ChooseRandomCombination();

                        //craetes the trajectory for both robots for the current trial if not one of the training protocols.
                        _currentTrialTrajectories = _trajectoryCreatorHandler.CreateTrajectory(_currentVaryingTrialIndex);

                        //reset the stickOnNumber.
                        _stickOnNumberIndex = 0;
                    }

                    //make that current option strickOn times one after one.
                    for (; _stickOnNumberIndex < NumOfStickOn;)
                    {
                        //if system has stopped , wait for the end of the current trial ans break,
                        if (Globals._systemState.Equals(SystemState.STOPPED) || _stopAfterTheEndOfTheCurrentTrial || _pauseAfterTheEndOfTheCurrentTrial)
                        {
                            Globals._systemState = SystemState.STOPPED;

                            if (!_pauseAfterTheEndOfTheCurrentTrial)
                                EndControlLoopByStopFunction();

                            return;
                        }

                        //Sending all needed data to all interfaces and makes the beep sound.
                        PreTrialStage();

                        //TODO:WAIT FOR THE START PRESS BUTTON
                        bool subjectPressedStartButtonDuringTheTimeoutDuration = WaitForStartButtonToBePressed();

                        //TODO:DELETE
                        //indicates if during duration1 time - the rat stays with the eyes in the center.
                        //bool duration1HeadInTheCenterStabilityStage = false;

                        //if the subject pressed the start button before timeOut time.
                        if (subjectPressedStartButtonDuringTheTimeoutDuration)
                        {
                            //TODO:DELETE THE CONDITION
                            //if the rat head was stable in the center for the startDelay time as required start the movement.
                            //if (duration1HeadInTheCenterStabilityStage = CheckDuration1HeadInTheCenterStabilityStage())
                            {
                                //update the state of the rat decision.
                                _currentRatDecision = RatDecison.DurationTime;

                                //moving the robot with duration time , and checking for the stability of the head in the center.
                                if (MovingTheRobotDurationWithHeadCenterStabilityStage())
                                {
                                    //update the number of total head in the center with stability during the duration time.
                                    _totalHeadStabilityInCenterDuringDurationTime++;
                                    _currentRatDecision = RatDecison.PassDurationTime;

                                    //reward the rat in the center with water for duration of rewardCenterDuration for stable head in the center during the movement.
                                    RewardCenterStage(AutoReward, CenterRewardSound);

                                    //if not to skip all stages after the fixation stage.
                                    //if (!FixationOnlyMode)
                                    {
                                        //wait the rat to response to the movement during the response time.
                                        Tuple<RatDecison, bool> decision = ResponseTimeStage();

                                        //second reward stage (condition if needed in the stage) with false for second chance response.
                                        SecondRewardStage(decision, AutoReward, false);

                                        //if second oppertunity for choice after wrong choice is available.
                                        if (SecondResponseChance && !AutoReward && !decision.Item1.Equals(RatDecison.NoDecision) && decision.Item2 == false)
                                        {
                                            Tuple<RatDecison, bool> secondDecision = SecondChanceResponseTimeStage();

                                            //second reward stage with flag indicate that it was a second chance.
                                            SecondRewardStage(secondDecision, AutoReward, true);
                                        }
                                    }
                                }

                                //if fixation break
                                else
                                {
                                    //update the number of head fixation breaks.
                                    _totalHeadFixationBreaks++;
                                }

                                //Maayan - Do I need this?
                                //after the end of rewrad wait a time delay before backword movement to the home poistion.
                                //RewardToBackwardDelayStage();
                            }
                        }

                        //sounds the beep with the missing start gead in the center.
                        else
                        {
                            //Task.Run(() => { _windowsMediaPlayer.URL = _soundPlayerPathDB["WrongAnswer"]; _windowsMediaPlayer.controls.play(); });
                        }

                        //the post trial stage for saving the trial data and for the delay between trials.
                        //TODO: What changes need to be made?
                        bool duration1HeadInTheCenterStabilityStage = true; //to delete
                        if (PostTrialStage(duration1HeadInTheCenterStabilityStage))
                            _stickOnNumberIndex++;
                        else//if the the trual not succeed - choose another varting index randomly and not the same index (also , if stick number exists make it only of happens at the first time fot the stick on index).
                        {
                            if (_stickOnNumberIndex == 0)
                            {
                                //reset the current trial varying index because it was not successful.
                                _varyingIndexSelector.ResetTrialStatus(_currentVaryingTrialIndex);

                                //choose the random combination index for the current trial.
                                _currentVaryingTrialIndex = _varyingIndexSelector.ChooseRandomCombination();

                                //craetes the trajectory for both robots for the current trial if not one of the training protocols.
                                _currentTrialTrajectories = _trajectoryCreatorHandler.CreateTrajectory(_currentVaryingTrialIndex);
                            }

                        }
                    }
                }

                //reset all trials combination statuses for the next repetition.
                _varyingIndexSelector.ResetTrialsStatus();
                _repetitionIndex++;
            }

            //end of all trials and repetitions.
            EndControlLoopByStopFunction();
        }
        /// <summary>
        /// Initializes the variables , points , trajectories , random varibles ,  etc.
        /// </summary>
        public void ResetVariables()
        {
            //TODO : change the index of the trial to be identical to the trial number in the result file.
            _logger.Info("Initialization Stage of trial #" + (_totalHeadStabilityInCenterDuringDurationTime + 1));

#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            //update the global details listview with the current stage.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
            _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Current Stage", "Intialization");
#endif

            //determine all current trial timings and delays.
            _currentTrialTimings = DetermineCurrentTrialTimings();
            CheckDurationTimeAcceptableValue();

            //determine current stimulus type.
            _currentTrialStimulusType = DetermineCurrentStimulusType();

            //set the reposne to the stimulus direction as no entry to descision stage (and change it after if needed as well).
            _currentRatDecision = RatDecison.NoEntryToResponseStage;
            //set the auto option to default values.
            _autosOptionsInRealTime = new AutosOptions();
            //initialize the trial spcial mode options.
            _specialModesInRealTime = new SpecialModes();
            //initialize the trial sounds mode options.
            _soundsMode = new SoundsMode();

            _specialModesInRealTime.EnableRightLeftMustEquals = EnableRightLeftMustEquals;

            //reset the trial stopwatch and add the start event trial to the trial events list and timings.
            _controlLoopTrialTimer.Restart();
            _trialEventRealTiming.Clear();

            //TODO: Do I need this?
            //updatre the trial number for the motoman protocol file creator to send it to the alpha omega.
            //_motomanController.MotomanProtocolFileCreator.TrialNum = _totalHeadStabilityInCenterDuringDurationTime + 1;
            //the adiitiom of 1 is because the ++ of one of them is only at the end of the movement.
            //_motomanController.MotomanProtocolFileCreator.TrialNum = _totalHeadStabilityInCenterDuringDurationTime + _totalHeadFixationBreaks + 1;
        }

        /// <summary>
        /// Pre trial stage for sending all pre data for the current trial.
        /// </summary>
        public void PreTrialStage()
        {
            _logger.Info("Pre trial stage begin.");

#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            //update the global details listview with the current stage.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
            _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Current Stage", "Intialization");
#endif

            //initialize the current time parameters and all the current trial variables.
            ResetVariables();

            //updates the gui elements as the current trial parameters.
            UpdateGuiElements();

            _trialEventRealTiming.Add("TrialBegin", _controlLoopTrialTimer.ElapsedMilliseconds);

            //TODO:DELETE
            /* Task sendDataToRobotTask = new Task(() =>
             {
                 SendDataToRobots();
             });*/

            //TODO:DELETE
            /*Task SendDataToLedControllersTask = new Task(() =>
            {
                SendDataToLedControllers();
            });*/

            Task preTrialWaitingTask = new Task(() =>
            {
                Thread.Sleep((int)(1000 * _currentTrialTimings.wPreTrialTime));
            });

            //TODO:DELETE
            //sendDataToRobotTask.Start();
            //TODO:DELETE
            //SendDataToLedControllersTask.Start();

            preTrialWaitingTask.Start();

            //Maayan edit
            //Task.WaitAll(preTrialWaitingTask, sendDataToRobotTask, SendDataToLedControllersTask);
            Task.WaitAll(preTrialWaitingTask);
        }

        /// <summary>
        /// Updates the gui elements with the current trial options.
        /// </summary>
        private void UpdateGuiElements()
        {
            //show some trial details to the gui trial details panel.
            ShowTrialDetailsToTheDetailsListView();
            //show the global experiment details for global experiment details.
            ShowGlobalExperimentDetailsListView();
        }

        /// <summary>
        /// A stage the rat gets a clue where the correct answer is.
        /// </summary>
        public void CueSoundPlayer()
        {
            //todo:add all this logic in if statement for the sound should play.
            _logger.Info("CueSoundPlayer begin. EnableCueSoundInBothSide = " + (EnableCueSoundInBothSide & EnableGoCueSound) +
                         ";EnableCueSoundCorrectSide" + (EnableCueSoundCorrectSide & EnableGoCueSound) + ".");

#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            //update the global details listview with the current stage.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
                _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Current Stage",
                "Clue Stage");
#endif

            //todo:check if it should be here and not outside this function - because the correct answer shoule not determine in this function especailly.
            //determine the current trial correct answer.
            DetermineCurrentStimulusAnswer();

            //updates special modes according to the real time values.
            _soundsMode.EnableGoCueSound = EnableGoCueSound;
            _soundsMode.EnableCueSoundInBothSide = EnableCueSoundInBothSide;
            _soundsMode.EnableCueSoundInCorrectSide = EnableCueSoundCorrectSide;

            if (EnableGoCueSound)
            {
                //TODO: Do I need this?
                //write the go cue event to the AlphaOmega system.
                //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.GoCueSound);
                _trialEventRealTiming.Add("GoCueSound", _controlLoopTrialTimer.ElapsedMilliseconds);

                //make the sound.
                if (EnableCueSoundInBothSide)
                {
                    _logger.Info("Start playing EnableCueSoundInBothSide");

                    _windowsMediaPlayer.URL = _soundPlayerPathDB["Ding"];
                    _windowsMediaPlayer.controls.play();

                    _logger.Info("End playing EnableCueSoundInBothSide");
                }
                else if (EnableCueSoundCorrectSide)
                {
                    if (_correctDecision.Equals(RatDecison.Right))
                    {
                        _logger.Info("Start playing EnableCueSoundCorrectSide - Right");

                        _windowsMediaPlayer.URL = _soundPlayerPathDB["Ding-Right"];
                        _windowsMediaPlayer.controls.play();

                        _logger.Info("End playing EnableCueSoundInBothSide");
                    }

                    else if (_correctDecision.Equals(RatDecison.Left))
                    {
                        _logger.Info("Start playing EnableCueSoundCorrectSide - Left");

                        _windowsMediaPlayer.URL = _soundPlayerPathDB["Ding-Left"];
                        _windowsMediaPlayer.controls.play();

                        _logger.Info("End playing EnableCueSoundInBothSide");
                    }
                }
            }

            _logger.Info("CueSoundPlayer ended.");
        }

        /// <summary>
        /// Waiting the rat to response the movement direction abd update the _totalCorrectAnswers counter.
        /// <returns>The rat decision value and it's correctness.</returns>
        /// </summary>
        public Tuple<RatDecison, bool> ResponseTimeStage()
        {
            _logger.Info("ResponseTimeStage begin.");

            //if not trainig continue.
            if (GetVariableValue("STIMULUS_TYPE") == "0")
            {
                //delete that line for stim 0.
                //return new Tuple<RatDecison, bool>(RatDecison.NoDecision, false);
            }

#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            //update the global details listview with the current stage.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
            _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Current Stage", "Waiting for Response");
#endif

            //get the current stimulus direction.
            double currentHeadingDirection = double.Parse(GetVariableValue("HEADING_DIRECTION"));


            //make the sound of the clue (after that in the reward stage it was with the ckue sound delay - paralleled).
            //make it parallel to the response time duration.
            Task.Run(() => CueSoundPlayer());

            Stopwatch sw = new Stopwatch();
            sw.Start();
            _ratSampleResponseTimer.Elapsed += SetRatReponse;
            //time to wait for the moving rat response. if decided about a side so break and return the decision and update the _totalCorrectAnsers.
            while (sw.ElapsedMilliseconds < (int)(1000 * _currentTrialTimings.wResponseTime))
            {
                if (_currentRatResponse == (byte)RatDecison.Left)
                {
                    Console.Beep(5000, 500);

                    //increase the total choices for wrong or correct choices (some choices).
                    _totalChoices++;

                    //update the current rat decision state.
                    //Console.Beep(10000, 100);
                    _currentRatDecision = RatDecison.Left;

                    //add the response real time to the real times dictionary.
                    _trialEventRealTiming.Add("RatDecision", _controlLoopTrialTimer.ElapsedMilliseconds);

                    //TODO: Do I need this?
                    //write the event that te rat enter it's head to the left to the AlphaOmega.
                    //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.HeadEnterLeft);

                    if (_correctDecision.Equals(RatDecison.Left))
                    {
                        //increase the total correct answers.
                        _totalCorrectAnswers++;

                        //update the psycho online graph.
                        _onlinePsychGraphMaker.AddResult("Heading Direction", _currentTrialStimulusType, currentHeadingDirection, AnswerStatus.CORRECT);

                        _logger.Info("ResponseTimeStage ended. RatDecison = RatDecison.Left" + "; Correct = True.");
                        return new Tuple<RatDecison, bool>(RatDecison.Left, true);
                    }

                    _onlinePsychGraphMaker.AddResult("Heading Direction", _currentTrialStimulusType, currentHeadingDirection, AnswerStatus.WRONG);

                    if (EnableErrorSound)
                    {
                        //error sound if needed.
                        Task.Run(() =>
                        {
                            _logger.Info("Start playing error sound");

                            _windowsMediaPlayer.URL = _soundPlayerPathDB["WrongAnswer"];
                            _windowsMediaPlayer.controls.play();

                            _logger.Info("End playing error sound");
                        });

                        //todo: ask adam about this.
                        //don't make sound if it is auto reward because the rat dont need to choose.
                        if (!AutoReward)
                        {
                            //TODO: Do I need this?
                            //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.AudioWrong);
                            _trialEventRealTiming.Add("AudioWrong", _controlLoopTrialTimer.ElapsedMilliseconds);
                        }

                        _soundsMode.ErrorChoiceSoundOn = true;
                    }

                    _logger.Info("ResponseTimeStage ended. RatDecison = RatDecison.Left" + "; Correct = False.");
                    return new Tuple<RatDecison, bool>(RatDecison.Left, false);
                }

                else if (_currentRatResponse == (byte)RatDecison.Right)
                {
                    Console.Beep(10000, 500);

                    //update current rat decision state.
                    _currentRatDecision = RatDecison.Right;

                    //increase the total choices for wromg or correct choices (some choices).
                    _totalChoices++;

                    //add the response real time to the real times dictionary.
                    _trialEventRealTiming.Add("RatDecision", _controlLoopTrialTimer.ElapsedMilliseconds);
                    //Console.Beep(10000,100);

                    //TODO: Do I need this?
                    //write the event that te rat enter it's head to the right to the AlphaOmega.
                    //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.HeadEnterRight);

                    if (_correctDecision.Equals(RatDecison.Right))
                    {
                        //increase the total coreect answers.
                        _totalCorrectAnswers++;

                        //update the psycho online graph.
                        _onlinePsychGraphMaker.AddResult("Heading Direction", _currentTrialStimulusType, currentHeadingDirection, AnswerStatus.CORRECT);

                        _logger.Info("ResponseTimeStage ended. RatDecison = RatDecison.Right" + "; Correct = True.");
                        return new Tuple<RatDecison, bool>(RatDecison.Right, true);
                    }

                    //update the psycho online graph.
                    _onlinePsychGraphMaker.AddResult("Heading Direction", _currentTrialStimulusType, currentHeadingDirection, AnswerStatus.WRONG);

                    //error sound if needed.
                    if (EnableErrorSound)
                    {
                        Task.Run(() =>
                        {
                            _logger.Info("Start playing wrong answer");

                            _windowsMediaPlayer.URL = _soundPlayerPathDB["WrongAnswer"];
                            _windowsMediaPlayer.controls.play();

                            _logger.Info("End playing wrong answer");
                        });

                        //todo: ask adam about this.
                        //don't make sound if it is auto reward because the rat dont need to choose.
                        if (!AutoReward)
                        {
                            //TODO: Do I need this?
                            //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.AudioWrong);
                            _trialEventRealTiming.Add("AudioWrong", _controlLoopTrialTimer.ElapsedMilliseconds);
                        }

                        _soundsMode.ErrorChoiceSoundOn = true;
                    }

                    _logger.Info("ResponseTimeStage ended. RatDecison = RatDecison.Right" + "; Correct = False.");
                    return new Tuple<RatDecison, bool>(RatDecison.Right, false);
                }
            }
            _ratSampleResponseTimer.Elapsed -= SetRatReponse;

            _logger.Info("ResponseTimeStage ended. RatDecison = RatDecison.NoDecision; Correct = False.");
            _currentRatDecision = RatDecison.NoDecision;
            return new Tuple<RatDecison, bool>(RatDecison.NoDecision, false);
        }

        /// <summary>
        /// Waiting the rat to second chance response the movement direction with no updating the _totalCorrectAnswers counter and the result psycho graph.
        /// <returns>The rat decision value and it's correctness.</returns>
        /// </summary>
        public Tuple<RatDecison, bool> SecondChanceResponseTimeStage()
        {
            //Thread.Sleep(1000*(int)(_currentTrialTimings.wResponseTime));

#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            //update the global details listview with the current stage.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
            _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Current Stage", "Waiting for Second Chance Response");
#endif

            //save the second chance response is on.
            _specialModesInRealTime.SecondChoice = true;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            //time to wait for the moving rat response. if decided about a side so break and return the decision and update the _totalCorrectAnsers.
            while (sw.ElapsedMilliseconds < (int)(1000 * _currentTrialTimings.wResponseTime))
            {
                if (_currentRatResponse == (byte)_correctDecision)
                {
                    //write the event that te rat enter it's head to the left to the AlphaOmega.
                    if (_correctDecision == RatDecison.Left)
                    {
                        //TODO: Do I need this?
                        //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.HeadEnterLeftSecondChance);

                        //add the 2nd response real time to the real times dictionary.
                        _trialEventRealTiming.Add("RatSecondDecision", _controlLoopTrialTimer.ElapsedMilliseconds);

                        return new Tuple<RatDecison, bool>(RatDecison.Left, true);
                    }
                    else
                    {
                        //TODO: Do I need this?
                        //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.HeadEnterRightSecondChance);

                        //add the 2nd response real time to the real times dictionary.
                        _trialEventRealTiming.Add("RatSecondDecision", _controlLoopTrialTimer.ElapsedMilliseconds);

                        return new Tuple<RatDecison, bool>(RatDecison.Right, true);
                    }
                }
            }

            //if no decision or no error correction.
            _currentRatDecision = RatDecison.NoDecision;

            return new Tuple<RatDecison, bool>(RatDecison.NoDecision, false);
        }

        /// <summary>
        /// Reward water to the rat in the given position with during the given time long.
        /// </summary>
        /// <param name="position">The cellenoid position side to be opened.</param>
        /// <param name="rewardDuration">The duration the selected cellenoid eould be opened.</param>
        /// <param name="rewardDelay">The delay time before opening the selected cellenoid.</param>
        /// <param name="autoreward">Indecition if to give the reward with no delay.</param>
        /// <param name="autoRewardSound">Indication if to give the auto reward sound during the reward.</param>
        /// <param name="responseReward">Indicates if the reward is due to a response or due to the center stability stage.</param>
        public void Reward(RewardPosition position, double rewardDuration, double rewardDelay, bool autoreward = false, bool autoRewardSound = false, bool responseReward = false)
        {
            //if autoReward than play the sound in the slected side of the water reward in order to help the rat to understand the water reward side.
            _autosOptionsInRealTime.AutoRewardSound = autoRewardSound;
            if (autoRewardSound)
            {
                Task.Run(() =>
                {
                    _logger.Info("Start getting the reward position sound.");

                    //play the selected reward side mono sound.
                    switch (position)
                    {
                        case RewardPosition.Center:
                            //TODO: Do I need this?
                            //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.CenterRewardSound);
                            _trialEventRealTiming.Add("CenterRewardSound", _controlLoopTrialTimer.ElapsedMilliseconds);
                            _windowsMediaPlayer.URL = _soundPlayerPathDB["Ding"];
                            _windowsMediaPlayer.controls.play();
                            break;
                        case RewardPosition.Left:
                            //TODO: Do I need this?
                            //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.SideRewardSound);
                            _trialEventRealTiming.Add("SideRewardSound", _controlLoopTrialTimer.ElapsedMilliseconds);
                            _windowsMediaPlayer.URL = _soundPlayerPathDB["Ding-Left"];
                            _windowsMediaPlayer.controls.play();
                            break;
                        case RewardPosition.Right:
                            //TODO: Do I need this?
                            //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.SideRewardSound);
                            _trialEventRealTiming.Add("SideRewardSound", _controlLoopTrialTimer.ElapsedMilliseconds);
                            _windowsMediaPlayer.URL = _soundPlayerPathDB["Ding-Right"];
                            _windowsMediaPlayer.controls.play();
                            break;
                        default:
                            break;
                    }

                    _logger.Info("End getting the reward position sound.");
                });
            }

            //wait the reward delay time.
            Thread.Sleep((int)(rewardDelay * 1000));

            //if 0 dont even open the reward tupple.
            Task rewardTask = Task.Factory.StartNew(() =>
            {
                if (rewardDuration > 0)
                {
                    //open the center reward for the rat to be rewarded.
                    //after the reward duration time and than close it.
                    _logger.Info("Opening the water tupple");

                    //TODO: Do I need this?
                    //_rewardController.WriteSingleSamplePort(true, (byte)position);

                    //send the alpha omega that a reward is given.
                    SendAlphaOmegaRewardEvent(position);

                    //wait the reward time and fill the interactive water fill estimation panel.
                    _logger.Info("Start updating interactive water filling window");
                    _waterRewardFillingTimer.Start();
                    Thread.Sleep((int)(rewardDuration * 1000));
                    _waterRewardFillingTimer.Stop();
                    _logger.Info("End updating interactive water filling window");

                    //close again the reward port.
                    _logger.Info("Closing the water tupple");
                    //TODO: Do I need this?
                    //_rewardController.WriteSingleSamplePort(true, 0x00);
                }
            });

            Task clueDelayTask = null;
            //if the reward is due to the center stability stage - add clue if needed. Else, if it is due to response stage , don't add clue.
            if (!responseReward)
            {
                clueDelayTask = Task.Factory.StartNew(() =>
                {
                    if (!EnableGoCueSound) return;
                    if (!EnableCueSoundCorrectSide && !EnableCueSoundInBothSide) return;
                    //give the cue only if it is a cebter reward
                    if (!position.Equals(RewardPosition.Center)) return;
                    //and only if it is not a fixation only trial.
                    //TODO: Maayan - Do I need wClueDelay parameter?
                    /*if (!FixationOnlyMode)
                    {
                        Thread.Sleep((int)(1000 * _currentTrialTimings.wClueDelay));
                    }*/
                });
            }

            if (!responseReward)
            {
                clueDelayTask.Wait();
            }
            rewardTask.Wait();
        }

        /// <summary>
        /// Second reward stage according to the rat response for the stimulus direction.
        /// </summary>
        /// <param name="decision">The rat decision about the stimulus direction and if correct or not.</param>
        /// <param name="autoReward">Indicated if to give reward automatically in the motion side of the stimulus direction.</param>
        /// <param name="secondChance">Indicates if it was a second chance response.</param>
        public void SecondRewardStage(Tuple<RatDecison, bool> decision, bool autoReward = false, bool secondChance = false)
        {
            _logger.Info("SecondRewardStage begin. AutoReward = " + AutoReward + "; SecondChance =" + secondChance + ".");

            //check if the decision was correct and reward the rat according that decision.
            if (decision.Item2)
            {
                //reward the choosen side cellenoid.
                switch (decision.Item1)
                {
                    case RatDecison.Center:
                        RewardCenterStage(false, CenterRewardSound, secondChance, true);
                        break;
                    case RatDecison.Left:
                        RewardLeftStage(false, SideRewardSound, secondChance, true);
                        break;
                    case RatDecison.Right:
                        RewardRightStage(false, SideRewardSound, secondChance, true);
                        break;
                    default:
                        break;
                }
            }

            //if to give reward no matter whether the response was correct or not.
            else if (AutoReward)
            {
                //get the current stimulus direction.
                double currentHeadingDirection = double.Parse(GetVariableValue("HEADING_DIRECTION"));

                //determine the current stimulus direaction.
                RatDecison currentStimulationSide = (currentHeadingDirection == 0) ? (RatDecison.Center) : ((currentHeadingDirection > 0) ? (RatDecison.Right) : RatDecison.Left);

                //make the rat decision be correct answer.
                _currentRatDecision = _correctDecision;

                //give the reward according to the robot direction motion with no delay.
                switch (_correctDecision)
                {
                    case RatDecison.Center:
                        //RewardCenterStage(true , AutoRewardSound);
                        //make the reward randomly chosen right or left.
                        if (_inverseRRDecision)
                        {

                        }
                        break;

                    case RatDecison.Left:
                        RewardLeftStage(true, SideRewardSound, false, true);
                        break;

                    case RatDecison.Right:
                        RewardRightStage(true, SideRewardSound, false, true);
                        break;

                    default:
                        break;
                }

                //save the auto reward option true.
                _autosOptionsInRealTime.AutoReward = true;
            }

            _autosOptionsInRealTime.AutoReward = autoReward;

            _logger.Info("SecondRewardStage ended.");
        }

        /// <summary>
        /// The reward left stage is happening if the rat decide the correct stimulus side.
        /// <param name="autoReward">Indecation if to give the reward with no delay.</param>
        /// <param name="RewardSound">Indecation ig to give the reward sound during the reward.</param>
        /// <param name="secondChance">Indicate if it is reward for the second chance.</param>
        /// <param name="responseReward">Indicates if the reward is due to a response or due to the center stability stage.</param>
        /// </summary>
        public void RewardLeftStage(bool autoReward = false, bool RewardSound = false, bool secondChance = false, bool responseReward = false)
        {
            _logger.Info("RewardLeftStage begin with AutoReward  = " + autoReward + ".");

#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            //update the global details listview with the current stage.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
            _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Current Stage", "Getting Reward (Left)");
#endif
            //TODO: Maayan - Do I need this?
            /*if (!secondChance)
                Reward(RewardPosition.Left, _currentTrialTimings.wRewardLeftDuration, _currentTrialTimings.wRewardLeftDelay, autoReward, RewardSound, responseReward);
            else
                Reward(RewardPosition.Left, _currentTrialTimings.wRewardLeftDurationSecondChance, _currentTrialTimings.wRewardLeftDelaySecondChance, false, RewardSound, responseReward);
                */

            _logger.Info("RewardLeftStage ended.");
        }

        /// <summary>
        /// The reward right stage is happening if the rat decide the correct stimulus side.
        /// <param name="autoReward">Indecation if to give the reward with no delay.</param>
        /// <param name="RewardSound">Indecation ig to give the suto reard sound during the reward.</param>
        /// <param name="secondChance">Indicate if it is reward for the second chance.</param>
        /// <param name="responseReward">Indicates if the reward is due to a response or due to the center stability stage.</param>
        /// </summary>
        public void RewardRightStage(bool autoReward = false, bool RewardSound = false, bool secondChance = false, bool responseReward = false)
        {
            _logger.Info("RewardRightStage begin with AutoReward  = " + autoReward + ".");

#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            //update the global details listview with the current stage.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
            _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Current Stage", "Getting Reward (Right)");
#endif
            //TODO: Maayan - Do I need this?
            /*if (!secondChance)
                Reward(RewardPosition.Right, _currentTrialTimings.wRewardRightDuration, _currentTrialTimings.wRewardRightDelay, autoReward, RewardSound, responseReward);
            else
                Reward(RewardPosition.Right, _currentTrialTimings.wRewardRightDurationSecondChance, _currentTrialTimings.wRewardRightDelaySecondChance, false, RewardSound, responseReward);
                */

            _logger.Info("RewardRightStage ended.");
        }

        /// <summary>
        /// The reward center stage is happening if the rat head was consistently stable in the center during the movement.
        /// <param name="autoReward">Indecation if to give the reward with no delay.</param>
        /// <param name="autoRewardSound">Indecation ig to give the suto reard sound during the reward.</param>
        /// <param name="responseReward">Indicates if the reward is due to a response or due to the center stability stage.</param>
        /// </summary>
        public void RewardCenterStage(bool autoReward = false, bool autoRewardSound = false, bool secondChance = false, bool responseReward = false)
        {
            _logger.Info("RewardCenterStage begin with AutoReward  = " + autoReward + ".");

#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            //update the global details listview with the current stage.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
            _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Current Stage", "Getting Reward (Center)");
#endif
            //TODO: Maayan - Do I need this?
            //Reward(RewardPosition.Center, _currentTrialTimings.wRewardCenterDuration, _currentTrialTimings.wRewardCenterDelay, autoReward, autoRewardSound, responseReward);

            _logger.Info("RewardCenterStage ended.");
        }

        /// <summary>
        /// The stage for the time between the end of the reward to the beginnig moving tohe robot to it's home position.
        /// </summary>
        private void RewardToBackwardDelayStage()
        {
            _logger.Info("RewardToBackwardDelayStage begin.");

            //TODO: Maayan - Do I need this?
            /*Task waitRewardBackwardDelay = new Task(() =>
            {
                Thread.Sleep((int)(_currentTrialTimings.wRewardToBackwardDelay * 1000));
            });*/

            Task updateBackwordTrajectory = new Task(() =>
            {
                //wait the half of the time for the pause after the robt end moving forward (and than write the JBI file).
                Thread.Sleep((int)(250));

                _logger.Info("Updating backward trajectory file begin.");
                UpdateRobotHomePositionBackwordsJBIFile();
                _logger.Info("Updating backward trajectory file begin.");
            });

            //update the JBI file after the little delay.
            updateBackwordTrajectory.Start();
            //TODO: Maayan - Do I need this?
            //waitRewardBackwardDelay.Start();

            //wait the other half time and the JBI writing file time.
            //TODO: Maayan - Do I need this?
            //waitRewardBackwardDelay.Wait();
            updateBackwordTrajectory.Wait();
            _logger.Info("RewardToBackwardDelayStage ended.");
        }

        /// <summary>
        /// Moving the robot stage (it the subject pressed start in the timeOut time and was stable in the center for startDelay time).
        /// This function also , in paralleled to the robot moving , checks that the rat head was consistently in the center during the duration time of the movement time.
        /// </summary>
        /// <returns>True if the head was stable consistently in the center during the movement time.</returns>
        public bool MovingTheRobotDurationWithHeadCenterStabilityStage()
        {
            _logger.Info("Moving the robot with duration time and head center stability check stage is begin.");

#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            //update the global details listview with the current stage.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
            _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Current Stage", "Stimulus Duration");
#endif

            //start moving the robot according to the stimulus type.
            _logger.Info("Send Executing robot trajectory data start command");
            //TODO:DELETE
            //_robotMotionTask.Start();

            //TODO: Maayan - call the Moog to make a move
            int movementDuration = (int)(1000 * _currentTrialTimings.wDuration);
            _robotMotionTask = Task.Factory.StartNew(() => Thread.Sleep(movementDuration));
            Task.Run(() =>
            {
                //for (_currentTrialTrajectories.Moog.count)
                int currentTrialTrajectoriesSize = _currentTrialTrajectories.Item1.Count();
                for (int i = 0; i < currentTrialTrajectoriesSize; i++)
                {
                    //SendPosition(currentTrialTrajectory.Moog(i).X , currentTrialTrajectory.Moog(i).Y , currentTrialTrajectory.Moog(i).Z)
                    double MOTION_BASE_CENTER = -0.22077500;
                    double surge = _currentTrialTrajectories.Item1[i].X;
                    double lateral = _currentTrialTrajectories.Item1[i].Y;
                    double heave = _currentTrialTrajectories.Item1[i].Z + MOTION_BASE_CENTER;
                    double rx = _currentTrialTrajectories.Item1[i].RX;
                    double ry = _currentTrialTrajectories.Item1[i].RY;
                    double rz = _currentTrialTrajectories.Item1[i].RZ;
                    GuiInterface.SendPosition(surge / 100.0, heave, lateral / 100.0, rx, ry, rz);
                }
            });

            //throw new Exception();

            //TODO:DELETE
            //write alpha omega that the stimulus start.
            /*Task.Run(() =>
            {
                WriteAlphaOmegaStimulusBegin();
                _trialEventRealTiming.Add("StimulusStart", _controlLoopTrialTimer.ElapsedMilliseconds);
            });*/

            //execute the leds command if necessary.
            if (_currentTrialStimulusType == 2 ||
                _currentTrialStimulusType == 3 ||
                _currentTrialStimulusType == 4 ||
                _currentTrialStimulusType == 5)
            {
                //TODO:DELETE
                /*Task.Run(() =>
                {
                    ExecuteLedControllersCommand();
                });*/

                //TODO: Maayan - call the unity visualization
                /*Task.Run(() =>
                {
                    for ()
                    {
                        SendOculusFrame(currentTrialTrajectory.Unity);
                        Position++;
                    }
                );*/
                Task.Run(() =>
                {
                    //---data to send to the unity program (server)---
                    string msgToSend = "Space";
                    string jsonMsgToSend = JsonConvert.SerializeObject(msgToSend.ToArray());
                    TCPSender.SendString(jsonMsgToSend);
                    TCPSender.SendMovement(_currentTrialTrajectories.Item1);
                });
            }

            //TODO:DELETE ALL BLOCK
            //also run the rat center head checking in parallel to the movement time.
            bool headInCenterAllTheTime = true;
            /*Task.Run(() =>
            {
                while (!_robotMotionTask.IsCompleted)
                {
                    //sample the signal indicating if the rat head is in the center only 60 time per second (because the refresh rate of the signal is that frequency).
                    Thread.Sleep((int)(Properties.Settings.Default.NoldusRatReponseSampleRate));
                    if (_currentRatResponse != 2 && headInCenterAllTheTime)
                    {
                        _logger.Info("Break fixation during the movement.");

                        headInCenterAllTheTime = false;

                        if (!AutoFixation)
                        {
                            if (EnableFixationBreakSound)
                            //sound the break fixation sound - aaaahhhh sound.
                            {
                                Task.Run(() =>
                                {
                                    _logger.Info("Start playing the missing answer sound");

                                    _windowsMediaPlayer.URL = _soundPlayerPathDB["MissingAnswer"];
                                    _windowsMediaPlayer.controls.play();

                                    _logger.Info("End playing the missing answer sound");
                                });
                            }

                            //save the state of the enable fixation break sound on.
                            _soundsMode.BreakFixationSoundOn = EnableFixationBreakSound;

                            //write the break fixation event to the AlphaOmega.
                            //TODO: Do I need this?
                            //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.HeadStabilityBreak);
                            _trialEventRealTiming.Add("HeadStabilityBreak", _controlLoopTrialTimer.ElapsedMilliseconds);
                        }
                        _autosOptionsInRealTime.AutoFixation = AutoFixation;
                    }
                }
            });*/

            //wait the robot task to finish the movement.
            if (_currentTrialStimulusType != 0)
            {
                _robotMotionTask.Wait();
            }
            //TODO: ADD THE SAME WAIT FOR THE VISUAL

            //TODO:DELETE
            //also send the AlphaOmega that motion forward ends.
            //TODO: Do I need this?
            //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.RobotEndMovingForward);
            _trialEventRealTiming.Add("RobotEndMovingForward", _controlLoopTrialTimer.ElapsedMilliseconds);

            //TODO:DELETE
            //_logger.Info("End MovingTheRobotDurationWithHeadCenterStabilityStage with AutoFixation = " + AutoFixation + ".");
            //return the true state of the heading in the center stability during the duration time or always true when AutoFixation.
            return headInCenterAllTheTime || AutoFixation;
        }

        /// <summary>
        /// Stage to check (after the rat enter the head to the center) that the head is stable in the center for startDelay time.
        /// </summary>
        /// <returns></returns>
        public bool CheckDuration1HeadInTheCenterStabilityStage()
        {
            _logger.Info("Head in the center stability stage is begin.");

            //waits the startdelay time before starting the motion of the robot for the rat to ensure stability with head in the center.
            //reset the stopwatch for new measurement time cycle of startDelay.
            Stopwatch sw = new Stopwatch();
            sw.Start();

            //check if the head is stable in the center during the startDelay time (before starting the movement).
            while (sw.ElapsedMilliseconds < (int)(_currentTrialTimings.wStartDelay * 1000))
            {
                //todo: add this block in a function called CheckBreakFixation() and also add this function in the block of function MovingTheRobotDurationWithHeadCenterStabilityStage instead the block inside this function.
                //if AutoFixation no need to check that.
                if (!AutoFixation)
                {
                    //sample the signal indicating if the rat head is in the center only 60 time per second (because the refresh rate of the signal is that frequency).
                    Thread.Sleep((int)(Properties.Settings.Default.NoldusRatReponseSampleRate));

                    //if the head sample mentioned that the head was not in the center during the startDelay time , break , and move to the post trial time.
                    if (_currentRatResponse != 2)
                    {
                        _logger.Info("Breaking head fixation during the stability stage occured.");


                        if (EnableFixationBreakSound)
                        //sound the break fixation sound - aaaahhhh sound.
                        {
                            Task.Run(() =>
                            {
                                _logger.Info("Start playing the missing answer sound");

                                _windowsMediaPlayer.URL = _soundPlayerPathDB["MissingAnswer"];
                                _windowsMediaPlayer.controls.play();

                                _logger.Info("End playing the missing answer sound");
                            });
                        }

                        //save the state of the enable fixation break sound on.
                        _soundsMode.BreakFixationSoundOn = EnableFixationBreakSound;
                        _autosOptionsInRealTime.AutoFixation = AutoFixation;

                        //write the break fixation event to the AlphaOmega.
                        //TODO: Do I need this?
                        //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.HeadStabilityBreak);
                        _trialEventRealTiming.Add("HeadStabilityBreak", _controlLoopTrialTimer.ElapsedMilliseconds);

                        //increase the head fixation breaks in start delay counter.
                        _totalHeadFixationBreaksStartDelay++;

                        return false;
                    }
                }
            }

            _logger.Info("Heading stability stage successfull with AutoFixation = " + AutoFixation + ".");

            return true;
        }

        /// <summary>
        /// Wait for thesubject to press the start button in order to start the movement.
        /// </summary>
        /// <returns>True if the subject pressed on the start button during the limit of the timeoutTime.</returns>
        //public bool WaitForHeadEnteranceToTheCenterStage()
        public bool WaitForStartButtonToBePressed()
        {
            //Sounds the start beep. Now waiting for the subject to press start button
            Task.Run(() =>
            {
                Console.Beep(2000, 200);
            });

            //TODO: Do I need this?
            //write the beep start event to the AlphaOmega.
            //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.AudioStart);
            _trialEventRealTiming.Add("AudioStart", _controlLoopTrialTimer.ElapsedMilliseconds);

            _logger.Info("Waiting for start button to be pressed.");

#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            //update the global details listview with the current stage.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
            _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Current Stage", "Waiting for rat to start trial");
#endif

            //if autoStart is checked than should not wait for the subject to press start for starting.
            _autosOptionsInRealTime.AutoStart = AutoStart;
            if (AutoStart)
            {
                _logger.Info("AutoStart is On so no wait.");

                return true;
            }

            //stopwatch for the start button response timeout.
            Stopwatch sw = new Stopwatch();
            sw.Start();

            while ((int)sw.Elapsed.TotalMilliseconds < (int)(_currentTrialTimings.wTimeOutTime * 1000))
            {

                if (_remoteController.IsStartButtonPressed())
                {
                    _trialEventRealTiming.Add("HeadEnterCenter", _controlLoopTrialTimer.ElapsedMilliseconds);

                    return true;
                }

                //sample the signal indicating if the subject pressed start only 60 time per second (because the refresh rate of the signal is that frequency).
                Thread.Sleep((int)(Properties.Settings.Default.NoldusRatReponseSampleRate));
            }

            return false;
        }

        /// <summary>
        /// The post trial time - analyaing the response , saving all the trial data into the results file.
        /// <param name="duration1HeadInTheCenterStabilityStage">Indicated if one of the robots ot both moved due to suration 1 head in the center stability stage.</param>
        /// <returns>True if trial succeed otherwise returns false.</returns>
        /// </summary>
        public bool PostTrialStage(bool duration1HeadInTheCenterStabilityStage)
        {
            _logger.Info("PostTrialStage begin.");

            //update the global details listview with the current stage.

#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
            _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Current Stage", "Post trial time.");
#endif
            //need to get the robot backword only if ther was a rat enterance that trigger thr robot motion.
            Task moveRobotHomePositionTask;
            if (!duration1HeadInTheCenterStabilityStage)
            {
                _logger.Info("Backward not executed because duration1HeadInTheCenterStabilityStage = false. Calling NullFunction().");
                moveRobotHomePositionTask = Task.Factory.StartNew(() => NullFunction());
            }
            else
            {
                //send the AlphaOmega that motion backward starts.
                //TODO: Do I need this?
                //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.RobotStartMovingBackward);
                _trialEventRealTiming.Add("RobotStartMovingBackward", _controlLoopTrialTimer.ElapsedMilliseconds);


                _logger.Info("Creating backward traj.");
                //TODO: Do I need this? yes
                Tuple<Trajectory, Trajectory> returnTrajectory = _trajectoryCreatorHandler.CreateTrajectory(_currentVaryingTrialIndex, true);
                _logger.Info("Finish creating backward traj.");

                moveRobotHomePositionTask = Task.Factory.StartNew(() =>
                {
                    //for (_currentTrialTrajectories.Moog.count)
                    _logger.Info("Backward started.");

                    int rtuenTrajectorySize = returnTrajectory.Item1.Count();
                    for (int i = 0; i < rtuenTrajectorySize; i++)
                    {
                        //SendPosition(currentTrialTrajectory.Moog(i).X , currentTrialTrajectory.Moog(i).Y , currentTrialTrajectory.Moog(i).Z)
                        double MOTION_BASE_CENTER = -0.22077500;
                        int size = _currentTrialTrajectories.Item1.X.Count - 1;
                        double surge = _currentTrialTrajectories.Item1[size].X + returnTrajectory.Item1[i].X;
                        double lateral = _currentTrialTrajectories.Item1[size].Y + returnTrajectory.Item1[i].Y;
                        double heave = returnTrajectory.Item1[i].Z + MOTION_BASE_CENTER;
                        double rx = returnTrajectory.Item1[i].RX;
                        double ry = returnTrajectory.Item1[i].RY;
                        double rz = returnTrajectory.Item1[i].RZ;
                        GuiInterface.SendPosition(surge / 100.0, heave, lateral / 100.0, rx, ry, rz);
                    }

                    _logger.Info("Backward ended.");
                });
            }

            Thread.Sleep(5000);

            //save the fixation only mode
            _specialModesInRealTime.FixationOnly = FixationOnlyMode;

            bool trialSucceed = true;
            //if no answer in the response time or not even coming to tge response time.
            if (!FixationOnlyMode && !(_currentRatDecision.Equals(RatDecison.Left) || _currentRatDecision.Equals(RatDecison.Right)))
                //reset status of the current trial combination index if there was no reponse stage at all.
                //_varyingIndexSelector.ResetTrialStatus(_currentVaryingTrialIndex);
                trialSucceed = false;

            if (FixationOnlyMode && !_currentRatDecision.Equals(RatDecison.PassDurationTime))
                //reset status of the current trial combination index if there was no reponse stage at all.
                //_varyingIndexSelector.ResetTrialStatus(_currentVaryingTrialIndex);
                trialSucceed = false;

            //save the data into the result file only if the trial is within success trials (that have any stimulus)
            if (!_currentRatDecision.Equals(RatDecison.NoEntryToResponseStage))
            {
                Task.Run(() =>
                {
                    _logger.Info("Saving trial# " + (_totalHeadStabilityInCenterDuringDurationTime + _totalHeadFixationBreaks) + " to the result file.");
                    _savedExperimentDataMaker.SaveTrialDataToFile(new TrialData()
                    {
                        StaticVariables = _staticVariablesList,
                        VaryingVariables = _crossVaryingVals[_currentVaryingTrialIndex],
                        TimingsVariables = _currentTrialTimings,
                        ApplicationVersionNumber = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                        ProtocolName = ProtocolFullName,
                        RatName = RatName,
                        StudentName = StudentName,
                        RatDecison = _currentRatDecision,
                        TrialNum = _totalHeadStabilityInCenterDuringDurationTime + _totalHeadFixationBreaks,
                        StickOnNumber = NumOfStickOn,
                        NumOfRepetitions = NumOfRepetitions,
                        AutosOptions = _autosOptionsInRealTime,
                        SpecialModes = _specialModesInRealTime,
                        SoundsMode = _soundsMode,
                        LedsData = new LedsData { TurnsOnPercentage = PercentageOfTurnedOnLeds, Brightness = LEDBrightness, RedValue = LEDcolorRed, GreenValue = LEDcolorGreen, BlueValue = LEDcolorBlue },
                        TrialEventsTiming = _trialEventRealTiming,
                        TotalHabdRewardTime = _handRewardTotalTimer.ElapsedMilliseconds
                    });
                });
            }

            Thread.Sleep((int)(_currentTrialTimings.wPostTrialTime * 1000));

            //wait the maximum time of the postTrialTime and the going home position time.
            //TODO: Do I need this?
            moveRobotHomePositionTask.Wait();
            //also send the AlphaOmega that motion backwards ends.
            //TODO: Do I need this?
            //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.RobotEndMovingBackward);

            //throw new Exception();

            _logger.Info("PostTrialStage ended. TrialSucceed = " + trialSucceed + ".");
            return trialSucceed;
        }
        #endregion

        #region STAGES_ADDIION_FUNCTION
        /// <summary>
        /// Sending the leds data to the leds controllers (without execution).
        /// </summary>
        private void SendDataToLedControllers()
        {
            _logger.Info("Send data to LEDs controller begin.");

            //TODO: Do I need this?
            //LEDsData ledsDataRight;
            //LEDsData ledsDataLeft;

            double coherenceLeftStrip = double.Parse(GetVariableValue("COHERENCE_LEFT_STRIP")) / 100;
            double coherenceRightStrip = double.Parse(GetVariableValue("COHERENCE_RIGHT_STRIP")) / 100;
            int flickerLeft = int.Parse(GetVariableValue("FLICKER_LEFT"));
            int flickerRight = int.Parse(GetVariableValue("FLICKER_RIGHT"));

            //The motion of the Yasakawa robot if needed as the current stimulus type (if is both visual&vestibular -3 or only vistibular-1).
            switch (_currentTrialStimulusType)
            {
                case 0://none
                    break;

                case 1://vistibular only.
                    break;

                case 2://visual only.
                case 3://vistibular and visual both.
                case 4://vistibular and visual both with delta+ for visual.
                       //TODO: Do I need this?
                       /*case 5://vistibular and visual both with delta+ for vistibular.
                           ledsDataRight = new LEDsData((byte)LEDBrightness, (byte)(LEDcolorRed), (byte)(LEDcolorGreen), (byte)(LEDcolorBlue), _ledSelectorRight.FillWithBinaryRandomCombination(PercentageOfTurnedOnLeds, coherenceRightStrip, flickerRight));
                           _ledControllerRight.LEDsDataCommand = ledsDataRight;
                           _ledControllerRight.SendData();
                           ledsDataLeft = new LEDsData((byte)LEDBrightness, (byte)(LEDcolorRed), (byte)(LEDcolorGreen), (byte)(LEDcolorBlue), _ledSelectorLeft.FillWithBinaryRandomCombination(PercentageOfTurnedOnLeds, coherenceLeftStrip, flickerLeft));
                           _ledControllerLeft.LEDsDataCommand = ledsDataLeft;
                           _ledControllerLeft.SendData();
                           break;*/

                case 10://visual only in the dark.
                case 12://will replace visual only in the dark.
                    break;

                case 11://combined in the dark.
                case 13://will replace combined in the dark.
                case 14://vistibular and visual both with delta+ for visual in the dark.
                case 15://vistibular and visual both with delta+ for vistibular in the dark.
                    break;

                default://if there is no motion , make a delay of waiting the duration time (the time that should take the robot to move).
                    break;
            }

            _logger.Info("Send data to LEDs controller end.");
        }

        /// <summary>
        /// Executing the leds command (tell the controllers to execute the commands have sent to them before).
        /// </summary>
        private void ExecuteLedControllersCommand()
        {
            Task.Run(() =>
            {
                _logger.Info("New Data Leds Execution for COHERENCE frame for LedController1");
                //TODO: Do I need this?
                //_ledControllerRight.ExecuteAllFrames();
            });

            Task.Run(() =>
            {
                _logger.Info("New Data Leds Execution for COHERENCE frame for LedController2");
                //TODO: Do I need this?
                //_ledControllerLeft.ExecuteAllFrames();
            });
        }

        /// <summary>
        /// Writing the stimulus type to the AlphaOmega according to the current stimulus type.
        /// </summary>
        private void WriteAlphaOmegaStimulusBegin()
        {
            _logger.Info("Writing AlphaOmega stimulus event start");

            switch (_currentTrialStimulusType)
            {
                case 0://none
                    break;
                case 1://vistibular only.
                    //TODO: Do I need this?
                    //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.StimulusStart1);
                    break;

                case 2://visual only.
                    //TODO: Do I need this?
                    //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.StimulusStart2);
                    break;

                case 3://vistibular and visual both.
                    //TODO: Do I need this?
                    //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.StimulusStart3);
                    break;

                case 4://vistibular and visual both with delta+ for visual.
                    //TODO: Do I need this?
                    //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.StimulusStart4);
                    break;

                case 5://vistibular and visual both with delta+ for vistibular.
                    //TODO: Do I need this?
                    //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.StimulusStart5);
                    break;

                case 10://visual only in the dark.
                case 12://will replace visual only in the dark.
                    //TODO: Do I need this?
                    //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.StimulusStart10);
                    break;

                case 11://combined in the dark.
                case 13://will replace combined in the dark.
                    //TODO: Do I need this?
                    //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.StimulusStart11);
                    break;

                case 14://vistibular and visual both with delta+ for visual in the dark.
                    //TODO: Do I need this?
                    //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.StimulusStart14);
                    break;
                case 15://vistibular and visual both with delta+ for vistibular in the dark.
                    //TODO: Do I need this?
                    //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.StimulusStart15);
                    break;


                default://if there is no motion , make a delay of waiting the duration time (the time that should take the robot to move).
                    break;
            }
        }

        /// <summary>
        /// Sending the data trajectories to the robots according to the current trial stimulus type (without executing).
        /// </summary>
        private void SendDataToRobots()
        {
            int movementDuration = (int)(1000 * _currentTrialTimings.wDuration);

            _logger.Info("Sending trajectories data to robots begin.");
            //The motion of the Yasakawa robot if needed as the current stimulus type (if is both visual&vestibular -3 or only vistibular-1).
            switch (_currentTrialStimulusType)
            {
                case 0://none
                    _robotMotionTask = Task.Factory.StartNew(() => Thread.Sleep(movementDuration));
                    break;
                case 1://vistibular only.
                    //first update the JBI file in seperately  , and after that negin both moving the robot and play with the leds for percisely simulatenously.
                    //TODO: Do I need this?
                    //_motomanController.UpdateYasakawaRobotJBIFile(_currentTrialTrajectories, MotomanProtocolFileCreator.UpdateJobType.Both);
                    //_robotMotionTask = new Task(() => _motomanController.MoveYasakawaRobotWithTrajectory(YASKAWA_TRAJECTORY_MOVEMENTS_JOB_WAIT_BY_IO, movementDuration));
                    break;

                case 2://visual only.
                    //first update the JBI file in seperately  , and after that negin both moving the robot and play with the leds for percisely simulatenously.
                    //TODO: Do I need this?
                    //_motomanController.UpdateYasakawaRobotJBIFile(_currentTrialTrajectories, MotomanProtocolFileCreator.UpdateJobType.R2Only);
                    //_robotMotionTask = new Task(() => _motomanController.MoveYasakawaRobotWithTrajectory(YASKAWA_TRAJECTORY_MOVEMENTS_JOB_WAIT_BY_IO, movementDuration));
                    break;

                case 3://vistibular and visual both.
                    //first update the JBI file in seperately  , and after that negin both moving the robot and play with the leds for percisely simulatenously.
                    //TODO: Do I need this?
                    //_motomanController.UpdateYasakawaRobotJBIFile(_currentTrialTrajectories, MotomanProtocolFileCreator.UpdateJobType.R1Only);
                    //_robotMotionTask = new Task(() => _motomanController.MoveYasakawaRobotWithTrajectory(YASKAWA_TRAJECTORY_MOVEMENTS_JOB_WAIT_BY_IO, movementDuration));
                    break;

                case 4://vistibular and visual both with delta+ for visual.
                case 5://vistibular and visual both with delta+ for vistibular.
                case 14://vistibular and visual both with delta+ for visual in the dark.
                case 15://vistibular and visual both with delta+ for vistibular in the dark.
                    //first update the JBI file in seperately  , and after that negin both moving the robot and play with the leds for percisely simulatenously.
                    double deltaHeading = 0;
                    if (_staticVariablesList.ContainsKey("DELTA"))
                        deltaHeading = _staticVariablesList["DELTA"];
                    else if (_crossVaryingVals[_currentVaryingTrialIndex].Keys.Contains("DELTA"))
                        deltaHeading = _crossVaryingVals[_currentVaryingTrialIndex]["DELTA"];
                    //if delta is 0 move only the R1 robot.
                    //TODO: Do I need this?
                    //_motomanController.UpdateYasakawaRobotJBIFile(_currentTrialTrajectories, (deltaHeading != 0) ? MotomanProtocolFileCreator.UpdateJobType.Both : MotomanProtocolFileCreator.UpdateJobType.R1Only);
                    //_robotMotionTask = new Task(() => _motomanController.MoveYasakawaRobotWithTrajectory(YASKAWA_TRAJECTORY_MOVEMENTS_JOB_WAIT_BY_IO, movementDuration));
                    break;

                case 10://visual only in the dark.
                case 12://will replace visual only in the dark.
                    //first update the JBI file in seperately  , and after that negin both moving the robot and play with the leds for percisely simulatenously.
                    //TODO: Do I need this?
                    //_motomanController.UpdateYasakawaRobotJBIFile(_currentTrialTrajectories, MotomanProtocolFileCreator.UpdateJobType.R2Only);
                    //_robotMotionTask = new Task(() => _motomanController.MoveYasakawaRobotWithTrajectory(YASKAWA_TRAJECTORY_MOVEMENTS_JOB_WAIT_BY_IO, movementDuration));
                    break;

                case 11://combined in the dark.
                case 13://will replace combined in the dark.
                    //first update the JBI file in seperately  , and after that negin both moving the robot and play with the leds for percisely simulatenously.
                    //_motomanController.UpdateYasakawaRobotJBIFile(_currentTrialTrajectories, MotomanProtocolFileCreator.UpdateJobType.R1Only);
                    //_robotMotionTask = new Task(() => _motomanController.MoveYasakawaRobotWithTrajectory(YASKAWA_TRAJECTORY_MOVEMENTS_JOB_WAIT_BY_IO, movementDuration));
                    break;

                default://if there is no motion , make a delay of waiting the duration time (the time that should take the robot to move).
                    _robotMotionTask = new Task(() => Thread.Sleep((int)(1000 * _currentTrialTimings.wDuration)));
                    break;
            };
            _logger.Info("Sending trajectories data to robots end.");
        }

        //TODO: Maayan - determine whether the subject answer was correct
        /// <summary>
        /// Determine the current trial correct side.
        /// </summary>
        public void DetermineCurrentStimulusAnswer()
        {
            //if stim tupe 0 chose uniformly random side.
            if (GetVariableValue("STIMULUS_TYPE") == "0")
            {
                _correctDecision = (Bernoulli.Sample(0.5) == 1) ? RatDecison.Right : RatDecison.Left;
            }

            //get the current stimulus direction.
            double currentHeadingDirection = double.Parse(GetVariableValue("HEADING_DIRECTION"));

            //determine if the current stimulses sign are invered (may be dur to the delta protocol and etc).
            bool inversedStimulusesSign = IsVisualAndVistibularInversesSign();

            //determine the current stimulus direaction.
            RatDecison currentStimulationSide = (currentHeadingDirection == 0) ? (RatDecison.Center) : ((currentHeadingDirection > 0) ? (RatDecison.Right) : RatDecison.Left);
            //determine if the current stimulus heading direction is in the random heading direction region.
            if (Math.Abs(currentHeadingDirection) <= double.Parse(_variablesList._variablesDictionary["RR_HEADINGS"]._description["parameters"]._MoogParameter)
                || (inversedStimulusesSign && EnableRRDelta))
            {
                //get a random side with probability of RR_PROBABILITY to the right side.
                int sampledBernouli = Bernoulli.Sample(double.Parse(_variablesList._variablesDictionary["RR_PROBABILITY"]._description["parameters"]._MoogParameter));

                RatDecison changedStimulusSide = currentStimulationSide;

                if (sampledBernouli == 1)
                {
                    currentStimulationSide = RatDecison.Right;
                }
                else
                {
                    currentStimulationSide = RatDecison.Left;
                }

                if (changedStimulusSide.Equals(currentStimulationSide))
                {
                    _inverseRRDecision = true;
                }
            }
            else
            {
                _inverseRRDecision = false;
            }

            //update the specail mode RRInverse parameter.
            _specialModesInRealTime.RRInverse = _inverseRRDecision;

            _correctDecision = currentStimulationSide;
        }

        /// <summary>
        /// Indicates if the current trial visual heading and visttibular heading are different in the sign.
        /// </summary>
        /// <returns>True if headings are inversed , otherwise false.</returns>
        private bool IsVisualAndVistibularInversesSign()
        {
            switch ((int)(double.Parse(GetVariableValue("STIMULUS_TYPE"))))
            {
                case 4://stimulus with delta+ for visual.
                case 5://stimulus with delta+ for vistibular.
                    //get the current stimulus direction.
                    double currentHeadingDirection = double.Parse(GetVariableValue("HEADING_DIRECTION"));
                    double delta = double.Parse(GetVariableValue("DELTA"));
                    if (Math.Abs(currentHeadingDirection) <= Math.Abs(delta / 2)) ;
                    return true;
                    return false;
                default: //other stimulses
                    return false;
            }
        }

        /// <summary>
        /// Return true if the duration time is according to the rules of the duration time variable.
        /// </summary>
        private void CheckDurationTimeAcceptableValue()
        {
            if (_currentTrialTimings.wDuration > 1.0)
            {
                MessageBox.Show("The current trial timing for robot movement is bigger than 1 second - Arduino doesnt have enough memory for more than 10 frames per second.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                throw new ArgumentException("The current trial timing for robot movement is bigger than 1 second - Arduino doesnt have enough memory for more than 10 frames per second.",
                    "_currentTrialTimings.wDuration");
            }

            if (_currentTrialTimings.wDuration * 1 - double.Parse(_currentTrialTimings.wDuration.ToString("0.0")) != 0)
            {
                MessageBox.Show("The current trial timing for robot movement is not a 0.1 multiplication than 1 second.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                throw new ArgumentException("The current trial timing for robot movement is not a 0.1 multiplication than 1 second.",
                    "_currentTrialTimings.wDuration");
            }
        }

        /// <summary>
        /// Send Alpha Omega that a reward is given in the specific position.
        /// </summary>
        /// <param name="position">The position the reward is giiven at.</param>
        private void SendAlphaOmegaRewardEvent(RewardPosition position)
        {
            switch (position)
            {
                //write that the rat get center reward to the AlphaOmega.
                case RewardPosition.Center:
                    //TODO: Do I need this?
                    //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.CenterReward);
                    _trialEventRealTiming.Add("CenterReward", _controlLoopTrialTimer.ElapsedMilliseconds);
                    break;

                //write that the rat get left reward to the AlphaOmega.
                case RewardPosition.Left:
                    //TODO: Do I need this?
                    //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.LeftReward);
                    _trialEventRealTiming.Add("LeftReward", _controlLoopTrialTimer.ElapsedMilliseconds);
                    break;

                //write that the rat get right reward to the AlphaOmega.
                case RewardPosition.Right:
                    //TODO: Do I need this?
                    //_alphaOmegaEventsWriter.WriteEvent(true, AlphaOmegaEvent.RightReward);
                    _trialEventRealTiming.Add("RightReward", _controlLoopTrialTimer.ElapsedMilliseconds);
                    break;

                default:
                    break;
            }
        }

        #endregion STAGES_ADDIION_FUNCTION

        #region GUI_CONTROLS_FUNCTIONS
        /// <summary>
        /// Show global experiment parameters.
        /// </summary>
        private void ShowGlobalExperimentDetailsListView()
        {
            //clear the past global details in the global details listview.
            _mainGuiInterfaceControlsDictionary["ClearGlobaExperimentlDetailsViewList"].BeginInvoke(
            _mainGuiControlsDelegatesDictionary["ClearGlobaExperimentlDetailsViewList"]);

            //update the number of trials count.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
                _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Trial # (count)", ((_totalHeadStabilityInCenterDuringDurationTime + _totalHeadFixationBreaks + _totalHeadFixationBreaksStartDelay + 1)).ToString());

            //update the number of total correct head in center with stabilty during duration time.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
                _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Success Trials", (_totalChoices).ToString());

            //update the number of total correct answers.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
                _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Correct Answers", (_totalCorrectAnswers).ToString());

            //update the number of total failure trial during duration time.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
                _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Failure Trials", (_totalHeadStabilityInCenterDuringDurationTime + _totalHeadFixationBreaks + _totalHeadFixationBreaksStartDelay - _totalChoices).ToString());

            //update the number of total fixation breaks.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
                _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Fixation Breaks", (_totalHeadFixationBreaks + _totalHeadFixationBreaksStartDelay).ToString());

            //update the number of left trials.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
                _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "Remaining Trials", (_varyingIndexSelector.CountRemaining()).ToString());

            //update the number of total correct head in center with stabilty during duration time.
            _mainGuiInterfaceControlsDictionary["UpdateGlobalExperimentDetailsListView"].BeginInvoke(
                _mainGuiControlsDelegatesDictionary["UpdateGlobalExperimentDetailsListView"], "No choices", (_totalHeadStabilityInCenterDuringDurationTime - _totalChoices).ToString());
        }

        /// <summary>
        /// Show the current trial dynamic details to the ListView.
        /// </summary>
        public void ShowTrialDetailsToTheDetailsListView()
        {
            Dictionary<string, double> currentTrialDetails = _crossVaryingVals[_currentVaryingTrialIndex];
            _mainGuiInterfaceControlsDictionary["ClearCurrentTrialDetailsViewList"].BeginInvoke(
            _mainGuiControlsDelegatesDictionary["ClearCurrentTrialDetailsViewList"]);

            foreach (string varName in currentTrialDetails.Keys)
            {
                string currentParameterDetails;
                //only ratHouseParameter
                currentParameterDetails = "[" + currentTrialDetails[varName].ToString() + "]";

                _mainGuiInterfaceControlsDictionary["UpdateCurrentTrialDetailsViewList"].BeginInvoke(
                _mainGuiControlsDelegatesDictionary["UpdateCurrentTrialDetailsViewList"], varName, currentParameterDetails);
            }
        }
        #endregion

        #region ADDITIONAL_FUNCTIONS
        /// <summary>
        /// This function is called if the control loop asked to be ended.
        /// </summary>
        public void EndControlLoopByStopFunction()
        {
            Globals._systemState = SystemState.FINISHED;

            //set robot's servo off.
            //TODO: Do I need this?
            //_motomanController.SetServoOff();

            //reset the repetition index.
            _repetitionIndex = 0;

            //show the final global experiment info (show it the last time)
            ShowGlobalExperimentDetailsListView();

            //Close and release the current saved file.
            _savedExperimentDataMaker.CloseFile();

            //#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            //raise an event for the GuiInterface that the trials round is over.
            _mainGuiInterfaceControlsDictionary["FinishedAllTrialsRound"].BeginInvoke(_mainGuiControlsDelegatesDictionary["FinishedAllTrialsRound"]);

            //choose none rat in the selected rat
            _mainGuiInterfaceControlsDictionary["ResetSelectedRatNameCombobox"].BeginInvoke(_mainGuiControlsDelegatesDictionary["ResetSelectedRatNameCombobox"]);
            //#endif
        }

        /// <summary>
        /// Load all mp3 files that the MediaPlayer object should use.
        /// </summary>
        private void LoadAllSoundPlayers()
        {
            _soundPlayerPathDB.Add("CorrectAnswer", Application.StartupPath + @"\SoundEffects\correct sound effect.wav");
            _soundPlayerPathDB.Add("WrongAnswer", Application.StartupPath + @"\SoundEffects\Wrong Buzzer Sound Effect (Raised pitch 400 percent and -3db).wav");
            _soundPlayerPathDB.Add("Ding", Application.StartupPath + @"\SoundEffects\Ding Sound Effects (raised pitch 900 percent).wav");
            _soundPlayerPathDB.Add("MissingAnswer", Application.StartupPath + @"\SoundEffects\Wrong Buzzer Sound Effect (Raised pitch 400 percent and -3db).wav");
            _soundPlayerPathDB.Add("Ding-Left", Application.StartupPath + @"\SoundEffects\Ding Sound Effects - Left (raised pitch 900 percent).wav");
            _soundPlayerPathDB.Add("Ding-Right", Application.StartupPath + @"\SoundEffects\Ding Sound Effects - Right (raised pitch 900 percent).wav");
        }

        /// <summary>
        /// Determine the current trial stimulus type bt the stimulus type variable status.
        /// </summary>
        /// <returns>The stimulus type.</returns>
        public int DetermineCurrentStimulusType()
        {
            string stimulusTypeStatus = _variablesList._variablesDictionary["STIMULUS_TYPE"]._description["status"]._MoogParameter;
            switch (stimulusTypeStatus)
            {
                case "1"://static
                    return int.Parse(_variablesList._variablesDictionary["STIMULUS_TYPE"]._description["parameters"]._MoogParameter);
                case "2"://varying
                case "6"://Vector
                    return (int)(_crossVaryingVals[_currentVaryingTrialIndex]["STIMULUS_TYPE"]);
            }
            return 0;
        }

        /// <summary>
        /// Detrmines all current tiral timings and delays acoording the time types statuses.
        /// </summary>
        /// <returns>Return the TrialTimings struct contains all the timings types.</returns>
        public TrialTimings DetermineCurrentTrialTimings()
        {
            TrialTimings currentTrialTimings;
            currentTrialTimings.wStartDelay = DetermineTimeByVariable("START_DELAY");

            // TODO: Maayan - Do I need this?
            /*currentTrialTimings.wRewardCenterDelay = DetermineTimeByVariable("REWARD_CENTER_DELAY");
            currentTrialTimings.wRewardRightDelay = DetermineTimeByVariable("REWARD_RIGHT_DELAY");
            currentTrialTimings.wRewardLeftDelay = DetermineTimeByVariable("REWARD_LEFT_DELAY");
            currentTrialTimings.wRewardLeftDelaySecondChance = DetermineTimeByVariable("REWARD_LEFT_DELAY_SC");
            currentTrialTimings.wRewardRightDelaySecondChance = DetermineTimeByVariable("REWARD_RIGHT_DELAY_SC");

            currentTrialTimings.wRewardCenterDuration = DetermineTimeByVariable("REWARD_CENTER_DURATION");
            currentTrialTimings.wRewardRightDuration = DetermineTimeByVariable("REWARD_RIGHT_DURATION");
            currentTrialTimings.wRewardLeftDuration = DetermineTimeByVariable("REWARD_LEFT_DURATION");
            currentTrialTimings.wRewardLeftDurationSecondChance = DetermineTimeByVariable("REWARD_LEFT_DURATION");
            currentTrialTimings.wRewardRightDurationSecondChance = DetermineTimeByVariable("REWARD_RIGHT_DURATION");

            currentTrialTimings.wRewardToBackwardDelay = DetermineTimeByVariable("REWARD_BACKWARD_TIME");*/

            currentTrialTimings.wPreTrialTime = DetermineTimeByVariable("PRE_TRIAL_TIME");

            currentTrialTimings.wPostTrialTime = DetermineTimeByVariable("POST_TRIAL_TIME");

            currentTrialTimings.wTimeOutTime = DetermineTimeByVariable("TIMEOUT_TIME");

            currentTrialTimings.wResponseTime = DetermineTimeByVariable("RESPONSE_TIME");

            currentTrialTimings.wDuration = DetermineTimeByVariable("STIMULUS_DURATION");

            // TODO: Maayan - Do I need this?
            //currentTrialTimings.wClueDelay = DetermineTimeByVariable("REWARD_CLUE_SOUND_DELAY");

            return currentTrialTimings;
        }

        /// <summary>
        /// determine the current trial of the input type time by it's status (random , static , etc...).
        /// </summary>
        /// <param name="timeVarName">The time type to be compute.</param>
        /// <returns>The result time according to the type of the time.</returns>
        public double DetermineTimeByVariable(string timeVarName)
        {

            //detrmine variable value by the status of the time type.
            string timeValue = GetVariableValue(timeVarName);

            //if not found - it is random type varriable.
            if (timeValue == string.Empty)
            {
                double lowTime = double.Parse(_variablesList._variablesDictionary[timeVarName]._description["low_bound"]._MoogParameter);
                double highTime = double.Parse(_variablesList._variablesDictionary[timeVarName]._description["high_bound"]._MoogParameter);
                return RandomTimeUniformly(lowTime, highTime);
            }

            return double.Parse(timeValue);
        }

        private List<string> GetVaryingVariablesList()
        {
            List<string> varyingVariablesNames = new List<string>();

            foreach (string item in _variablesList._variablesDictionary.Keys)
            {
                if (_variablesList._variablesDictionary[item]._description["status"]._MoogParameter[0].Equals("2"))
                {
                    varyingVariablesNames.Add(_variablesList._variablesDictionary[item]._description["nice_name"]._MoogParameter[0].ToString());
                }
            }

            return varyingVariablesNames;
        }

        /// <summary>
        /// Determines the asked variable value at the current stage by it's status (not include random types).
        /// </summary>
        /// <param name="parameterName">The parameter name to get the value for.</param>
        /// <returns>The value of the parameter at the current trial.</returns>
        public string GetVariableValue(string parameterName)
        {
            try
            {
                //detrmine the status of the variable type.
                string variableStatus = _variablesList._variablesDictionary[parameterName]._description["status"]._MoogParameter;


                //decide the time value of the time type according to it's status.
                switch (variableStatus)
                {
                    case "0"://const
                    case "1"://static
                        return _variablesList._variablesDictionary[parameterName]._description["parameters"]._MoogParameter;

                    case "2"://varying
                    case "6":
                        return _crossVaryingVals[_currentVaryingTrialIndex][parameterName].ToString("000000.00000000");

                    default:
                        return string.Empty;

                }
            }

            catch
            {
                MessageBox.Show("Error", "The parameter " + parameterName + " is not in the excel sheet.", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return string.Empty;
            }
        }

        /// <summary>
        /// Random a double (4 precision) value uniformly by the given bounds.
        /// </summary>
        /// <param name="lowTime">The low bound.</param>
        /// <param name="highTime">The high bound.</param>
        /// <returns>The random 4 double precision in the bounded range.</returns>
        public double RandomTimeUniformly(double lowTime, double highTime)
        {
            //we cannot really have a randon double number because in uniform countinious distrbution the probability for any value is 0.
            //so mutiplt it by 1000 , make a random number , and party it by 1000 (4 digits precision).
            int lowTimeInteger = (int)(lowTime * 1000);
            int highTimeInteger = (int)(highTime * 1000);

            //get the random integer (the doubled rand time in 4 digits precison).
            int randTimeInteger = _timingRandomizer.Next(lowTimeInteger, highTimeInteger);

            //return the result.
            return (double)(randTimeInteger) / 1000;
        }

        /// <summary>
        /// Null bumper function.
        /// </summary>
        private void NullFunction()
        {
        }

        /// <summary>
        /// Update the robot trajectory JBI file to it's home (origin) position.
        /// </summary>
        public void UpdateRobotHomePositionBackwordsJBIFile()
        {
            switch (_currentTrialStimulusType)
            {
                case 0://none
                    break;
                case 1://vistibular only.
                    //TODO: Do I need this?
                    //_motomanController.UpdateYasakawaRobotJBIFile(_currentTrialTrajectories, MotomanProtocolFileCreator.UpdateJobType.Both, true);
                    break;

                case 2://visual only.
                case 10://visual only in the dark.
                case 12://will replace visual only in the dark.
                    //TODO: Do I need this?
                    //_motomanController.UpdateYasakawaRobotJBIFile(_currentTrialTrajectories, MotomanProtocolFileCreator.UpdateJobType.R2Only, true);
                    break;

                case 3://vistibular and visual both.
                case 11://vistibular and visual both in the dark.
                case 13://will replace vistibular and visual both in the dark.
                    //TODO: Do I need this?
                    //_motomanController.UpdateYasakawaRobotJBIFile(_currentTrialTrajectories, MotomanProtocolFileCreator.UpdateJobType.R1Only, true);
                    break;

                case 4://vistibular and visual both with +delta for visual.
                case 5://vistibular and visual both with -delta for visual.
                case 14://vistibular and visual both with +delta for visual in the dark.
                case 15://vistibular and visual both with -delta for visual in the dark.
                    //move only R1 if delta is 0
                    double deltaHeading = 0;
                    if (_staticVariablesList.ContainsKey("DELTA"))
                        deltaHeading = _staticVariablesList["DELTA"];
                    else if (_crossVaryingVals[_currentVaryingTrialIndex].Keys.Contains("DELTA"))
                        deltaHeading = _crossVaryingVals[_currentVaryingTrialIndex]["DELTA"];
                    //TODO: Do I need this?
                    //_motomanController.UpdateYasakawaRobotJBIFile(_currentTrialTrajectories, (deltaHeading != 0) ? MotomanProtocolFileCreator.UpdateJobType.Both : MotomanProtocolFileCreator.UpdateJobType.R1Only, true);
                    break;

                default://if there is no motion , make a delay of waiting the duration time (the time that should take the robot to move).
                    break;
            }
        }

        /// <summary>
        /// Giving reward as specified (for the specified directions).
        /// </summary>
        /// <param name="value">The specified direction by xxxxxy-y-y where left-center-right.</param>
        /// <param name="continious">Make the reward continiously (open untill get a close value) or not continiously (by the time of REWARD_CENTER_DURATION parameter.</param>
        public void GiveRewardHandReward(byte value, bool continious = false)
        {
            if (continious)
            {
                //TODO: Do I need this?
                //_rewardController.WriteSingleSamplePort(true, value);

                //for the manual reward stopwatch accounting.
                if (value != 0)
                    _handRewardTotalTimer.Start();
                else
                    _handRewardTotalTimer.Stop();
            }
            else
            {
                //wait the delat time before opening the water and make a sound if AutoSound is on.
                double timeByVariable = DetermineTimeByVariable("REWARD_CENTER_DURATION");

                Task.Run(() =>
                {
                    if ((value & (byte)RatDecison.Left) == (byte)RatDecison.Left)
                    {
                        if (SideRewardSound)
                        {
                            _logger.Info("Start handreward sound - left");

                            _windowsMediaPlayer.URL = _soundPlayerPathDB["Ding-Left"];
                            _windowsMediaPlayer.controls.play();

                            _logger.Info("End handreward sound - left");
                        }
                        timeByVariable = DetermineTimeByVariable("REWARD_LEFT_DURATION");
                    }
                    else if ((value & 0x05) == 0x05)//if both sides (right and left).
                    {
                        if (CenterRewardSound)
                        {
                            _logger.Info("Start handreward sound - center (left and right)");

                            _windowsMediaPlayer.URL = _soundPlayerPathDB["Ding"];
                            _windowsMediaPlayer.controls.play();

                            _logger.Info("End handreward sound - center (left and right)");
                        }
                        timeByVariable = DetermineTimeByVariable("REWARD_CENTER_DURATION");
                    }
                    else if ((value & (byte)RatDecison.Center) == (byte)RatDecison.Center)
                    {
                        if (CenterRewardSound)
                        {
                            _logger.Info("Start handreward sound - center");

                            _windowsMediaPlayer.URL = _soundPlayerPathDB["Ding"];
                            _windowsMediaPlayer.controls.play();

                            _logger.Info("End handreward sound - center");
                        }
                        timeByVariable = DetermineTimeByVariable("REWARD_CENTER_DURATION");
                    }
                    else if ((value & (byte)RatDecison.Right) == (byte)RatDecison.Right)
                    {
                        if (SideRewardSound)
                        {
                            _logger.Info("Start handreward sound - right");

                            _windowsMediaPlayer.URL = _soundPlayerPathDB["Ding-Right"];
                            _windowsMediaPlayer.controls.play();

                            _logger.Info("End handreward sound - right");
                        }
                        timeByVariable = DetermineTimeByVariable("REWARD_RIGHT_DURATION");
                    }
                });

                //TODO: Do I need this?
                //_rewardController.WriteSingleSamplePort(true, value);
                _handRewardTotalTimer.Start();
                Thread.Sleep((int)(timeByVariable * 1000));
                _handRewardTotalTimer.Stop();
                //TODO: Do I need this?
                //_rewardController.WriteSingleSamplePort(true, 0);
            }
        }

        /// <summary>
        /// An event raises every [Properties.Settings.Default.NoldusRatReponseSampleRate] second for sampling the rat head direction.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">Args.</param>
        private void SetRatReponse(object sender, System.Timers.ElapsedEventArgs args)
        {
            //TODO: Do I need this?
            //update the variable saving the current rat head direction.
            //_currentRatResponse = _ratResponseController.ReadSingleSamplePort();
            // Maayan edit
            _currentRatResponse = _remoteController.SubjectMovementChoice();

            //Console.Beep(10000,500);

#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            //only if the system is running , update the interactive window.
            if(Globals._systemState.Equals(SystemState.RUNNING))
                _mainGuiInterfaceControlsDictionary["SetNoldusRatResponseInteractivePanel"].BeginInvoke(_mainGuiControlsDelegatesDictionary["SetNoldusRatResponseInteractivePanel"] , _currentRatResponse);
#endif
        }
        #endregion

        #region EVENTS
        /// <summary>
        /// Handler for raising interval time evemt for the water fill estimation panel.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Args.</param>
        void WaterRewardFillingTimer_Tick(object sender, EventArgs e)
        {
#if UPDATE_GLOBAL_DETAILS_LIST_VIEW
            _mainGuiInterfaceControlsDictionary["SetWaterRewardsMeasure"].BeginInvoke(
                _mainGuiControlsDelegatesDictionary["SetWaterRewardsMeasure"] , false);
#endif
        }
        #endregion

        #region GUI_EVENTS
        /// <summary>
        /// Plays the Ding sound file.
        /// </summary>
        public void PlayRewardSound()
        {
            _logger.Info("Start playing reward sound from gui");

            _windowsMediaPlayer.URL = _soundPlayerPathDB["Ding"]; _windowsMediaPlayer.controls.play();

            _logger.Info("End playing reward sound from gui");
        }

        /// <summary>
        /// Plays the WrongAnswer sound file.
        /// </summary>
        public void PlayBreakFixationSound()
        {
            _logger.Info("Start playing break fixation sound from gui");

            _windowsMediaPlayer.URL = _soundPlayerPathDB["WrongAnswer"]; _windowsMediaPlayer.controls.play();

            _logger.Info("End playing break fixation sound from gui");
        }
        #endregion GUI_EVENTS

        #region STRUCT_ENUMS
        /// <summary>
        /// Struct contains all the trial timings.
        /// </summary>
        public struct TrialTimings
        {
            /// <summary>
            /// The delay time between the rat head center and the trial start time.
            /// </summary>
            public double wStartDelay;

            //TODO: Maayan - Do I need all these parameters?
            /*
            /// <summary>
            /// The delay between the center head tracking (for the trial begin) and the center reward.
            /// </summary>
            public double wRewardCenterDelay;

            /// <summary>
            /// The delay between the right head tracking (for the trial begin) and the right reward.
            /// </summary>
            public double wRewardRightDelay;

            /// <summary>
            /// The delay between the left head tracking (for the trial begin) and the left reward.
            /// </summary>
            public double wRewardLeftDelay;

            /// <summary>
            /// The delay between the right head tracking (for the trial begin) and the right reward for the second chance.
            /// </summary>
            public double wRewardRightDelaySecondChance;

            /// <summary>
            /// The delay between the left head tracking (for the trial begin) and the left reward for the second chance.
            /// </summary>
            public double wRewardLeftDelaySecondChance;

            /// <summary>
            /// The duration for the center reward.
            /// </summary>
            public double wRewardCenterDuration;

            /// <summary>
            /// The duration for the right reward.
            /// </summary>
            public double wRewardRightDuration;

            /// <summary>
            /// The duration for the left reward.
            /// </summary>
            public double wRewardLeftDuration;

            /// <summary>
            /// The duration for the right reward second chance.
            /// </summary>
            public double wRewardRightDurationSecondChance;

            /// <summary>
            /// The duration for the left reward seconde chance.
            /// </summary>
            public double wRewardLeftDurationSecondChance;

            /// <summary>
            /// The delay between end of water reward to the begining of moving rovot to it's home position.
            /// </summary>
            public double wRewardToBackwardDelay;
            */

            /// <summary>
            /// The pre trial time before start beep and trial starts.
            /// </summary>
            public double wPreTrialTime;

            /// <summary>
            /// The duration to wait between the end of the previous trial and the begining of the next trial.
            /// </summary>
            public double wPostTrialTime;

            /// <summary>
            /// The time after the beep of the trial begin time and the time the rat can response with head to the center in order to begin the movement.
            /// </summary>
            public double wTimeOutTime;

            /// <summary>
            /// The time the rat have to response (with head to the left or to the right) after the rewardCenter (ig get).
            /// </summary>
            public double wResponseTime;

            /// <summary>
            /// The robot movement duration.
            /// </summary>
            public double wDuration;

            //TODO: Maayan - Do I need this parameter?
            /// <summary>
            /// The delay time between the end of water center reward and the Clue sound.
            /// </summary>
            //public double wClueDelay;
        };

        /// <summary>
        /// Enum for the rat stimulus decision.
        /// </summary>
        public enum RatDecison
        {
            /// <summary>
            /// The rat decision could not happened due to no enterance to the reponse stage (because no stability or enterance to the center).
            /// </summary>
            NoEntryToResponseStage = -1,

            /// <summary>
            /// The rat doesn't decide anything (it's head was not in any of the sides).
            /// </summary>
            NoDecision = 0,

            /// <summary>
            /// The rat decide about the center.
            /// </summary>
            Center = 7,

            /// <summary>
            /// The rat decide about the left side.
            /// </summary>
            Left = 1,

            /// <summary>
            /// The rat decide about the right side.
            /// </summary>
            Right = 2,

            /// <summary>
            /// The rat decide about the left side.
            /// </summary>
            Up = 3,

            /// <summary>
            /// The rat decide about the right side.
            /// </summary>
            Down = 4,

            /// <summary>
            /// The rat passed the duration time fixation (movement) as a state.
            /// </summary>
            PassDurationTime = 5,

            /// <summary>
            /// DurationTime state.
            /// </summary>
            DurationTime = 6
        };

        /// <summary>
        /// Enum for the reward poition/direction.
        /// </summary>
        public enum RewardPosition
        {
            /// <summary>
            /// Reward to the center.
            /// </summary>
            Center = 0x02,

            /// <summary>
            /// Reward to the left.
            /// </summary>
            Left = 0x01,

            /// <summary>
            /// Reward to the right.
            /// </summary>
            Right = 0x04,
        };
        #endregion
        #endregion FUNCTIONS
    }
}
