using Prism.Ioc;
using WPFUtilityApp.Views;
using System.Windows;
using Prism.Modularity;
using WPFUtilityApp.Modules.ModuleName;
using WPFUtilityApp.Services.Interfaces;
using WPFUtilityApp.Services;

namespace WPFUtilityApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IMessageService, MessageService>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<ModuleNameModule>();
        }
    }
}
