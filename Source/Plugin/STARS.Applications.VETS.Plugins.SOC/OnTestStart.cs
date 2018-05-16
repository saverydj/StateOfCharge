using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using STARS.Applications.Interfaces.Dialogs;
using STARS.Applications.Interfaces.EntityManager;
using STARS.Applications.VETS.Interfaces;
using STARS.Applications.VETS.Interfaces.Constants;
using STARS.Applications.VETS.Interfaces.Devices;
using STARS.Applications.VETS.Interfaces.Entities;
using STARS.Applications.VETS.Interfaces.Logging;
using STARS.Applications.VETS.Interfaces.TestExecution;
using STARS.Applications.VETS.Interfaces.TestExecution.Activities;
using STARS.Applications.VETS.Interfaces.TestExecution.Activities.Attributes;
using Stars.ApplicationManager;
using Stars.Resources;
using log4net;

namespace STARS.Applications.VETS.Plugins.SOC.Interface
{
    /// <summary>
    /// Activity to update VETS resources from VTS file
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [AsyncTestActivity(typeof(IEmissionTestRunContext),
        TriggerState = EmissionTestStates.PreTestConfiguration,
        BlockStateExit = EmissionTestStates.PreTestConfiguration)]
    internal class OnTestStart : IAsyncTestActivity<IEmissionTestRunContext>
    {

        #region Imports
#pragma warning disable 649
        [Import] internal IStarsApplication _starsApplication;
        [Import] internal ILocalResourceSupport _localResourceSupport;
        [Import] internal ITestStatus TestStatus;
        [Import] internal IEntityQuery EntityQuery;
        [Import] internal IVETSEntityManagerView EntityManagerView;
        [Import] internal IDialogService DialogService;
        [Import] internal ISystemLogManager SystemLogManager;
#pragma warning restore 649
        #endregion

        #region Implementation of IDisposable
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {

        }
        #endregion

        #region Implementation of IAsyncTestActivity
        /// <summary>
        /// Do all test stand updating and re-activate if there have been any changes
        /// </summary>
        /// <param name="runContext">The context to use to run the activity</param>
        /// <param name="activityStatus">Call methods on this object to indicate state and progress of activity</param>
        public void Run(IActivityRunContext<IEmissionTestRunContext> runContext, IActivityStatus activityStatus)
        {
            _runContext = runContext;
            _activityStatus = activityStatus;

            try
            {
                Test runningTest = EntityQuery.FirstOrDefault<Test>(x => x.ID == TestStatus.RunningTests.Last().RunningTestID);

                if (Config.ShowPopupOnTestStart && runningTest != null)
                {
                    List<PropertyInfo> properties = runningTest.GetType().GetProperties().ToList();
                    if (properties.Any(x => x.Name == "CycleRepeat"))
                    {
                        if (properties.FirstOrDefault(x => x.Name == "CycleRepeat").GetValue(runningTest, null).ToString() == "OperatorControlled")
                        {
                            ShowMessage(Config.PopupTitle, Config.PopupMessage);
                        }
                    }
                }
                _activityStatus.Completed();
            }
            catch (Exception e)
            {
                if (!_aborted)
                    _activityStatus.Failed(e);
            }
        }

        /// <summary>
        /// Abort the execution of this activity. Will only be called if the activity is running.
        /// </summary>
        /// <param name="runContext">The run context</param>
        public void Abort(IActivityRunContext<IEmissionTestRunContext> runContext)
        {
            _aborted = true;
        }

        private void ShowMessage(string title, string message)
        {
            var result = DialogService.PromptUser(
                title,
                string.Format(message),
                DialogIcon.Information,
                DialogButton.OK,
                DialogButton.OK);
        }


        /// <summary>
        /// An action that will rollback any actions that the activity has taken. This will only be called
        /// if the activity has run and completed.
        /// </summary>
        public Action<IActivityRunContext<IEmissionTestRunContext>, Action<Exception>> Rollback
        {
            get { return null; }
        }
        #endregion

        #region Fields

        protected static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IActivityRunContext<IEmissionTestRunContext> _runContext;

        private IActivityStatus _activityStatus;

        private bool _aborted;

        private readonly ActionExecutor _actionExecutor = new ActionExecutor();

        #endregion
    }
}
