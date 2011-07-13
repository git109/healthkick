using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Hardcodet.Wpf.TaskbarNotification;
using healthkick.DeviceMonitor;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace healthkick
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {

    private MediaEvent _mediaEvent = new MediaEvent();

    private void Application_Startup(object sender, StartupEventArgs e)
    {
      this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
      
      _mediaEvent.MediaWatcher += new MediaWatcherEventHandler(MeMediaWatcher);
      _mediaEvent.Monitor("K:", _mediaEvent);

      var dao = new DAO();
      var vm = new ViewModel.StickerViewModel(dao);
      vm.NewSticker();

      // Create system tray icon
      TaskbarIcon tbi = new TaskbarIcon();
      tbi.Icon = new Icon("DefaultTrayIcon.ico");
      tbi.ToolTipText = "HealthKick!";
      tbi.ContextMenu = new ContextMenu();
      MenuItem mi = new MenuItem();
      mi.Header = "Exit";
      mi.Click += new RoutedEventHandler(mi_Click);
      tbi.ContextMenu.Items.Add(mi);
      tbi.MenuActivation = PopupActivationMode.RightClick;

    }

    void mi_Click(object sender, RoutedEventArgs e)
    {
      Shutdown();
    }
    
    static void MeMediaWatcher(object sender, MediaEvent.DriveStatus driveStatus)
    {
      Console.WriteLine("Cool!" + driveStatus);
    }
    
  }
}
