using System.ComponentModel.Composition;
using System.Windows.Input;
using Caliburn.PresentationFramework.Views;
using STARS.Applications.Interfaces;
using STARS.Applications.Interfaces.Constants;
using STARS.Applications.Interfaces.ViewModels;
using STARS.Applications.UI.Common;
using STARS.Applications.VETS.Interfaces;
using STARS.Applications.VETS.Interfaces.Constants;
using STARS.Applications.VETS.Interfaces.ViewModels.Attributes;
using STARS.Applications.VETS.UI.Views.Commands.CommandBaseViews;
using System.Diagnostics;
using STARS.Applications.VETS.Interfaces.Entities;
using STARS.Applications.Interfaces.EntityManager;

namespace STARS.Applications.VETS.Plugins.SOC.ViewModels.Home
{
    [View(typeof(Explorer), Context = "Explorer")]
    [Command(CommandCategories.Utilities, "SOC", Priority = Priorities.Last),
     PartCreationPolicy(CreationPolicy.Shared)]
    class HomeViewCommandViewModel //: ICommandViewModel
    {

        [ImportingConstructor]
        public HomeViewCommandViewModel(IEntityQuery entityQuery, IImageManager imageManager, [Import("SOC", typeof(Program))] Program program)
        {
            DisplayName = Properties.Resources.DisplayName;
            DisplayInfo = new ExplorerDisplayInfo
            {
                Description = Properties.Resources.DisplayName,
                Image16 = "/STARS.Applications.VETS.Plugins.SOC;component/Images/color_image_16.png",
                ExplorerImage16 = "/STARS.Applications.VETS.SOC.Button;component/Images/white_image_16.png"
            };

            _entityQuery = entityQuery;
            Command = new RelayCommand(x => DebugSOCAnalysis());
        }

        private void DebugSOCAnalysis()
        {
            Test test = _entityQuery.FirstOrDefault<Test>();
            Program.GetSocDataInParallel(Config.PopupMessage, test);
        }

        public DisplayInfo DisplayInfo { get; private set; }
        public string DisplayName { get; private set; }
        public ICommand Command { get; private set; }
        private IEntityQuery _entityQuery;

    }
}
