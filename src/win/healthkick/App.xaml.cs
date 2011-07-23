using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using HealthKick.DeviceMonitor;
using HealthKick.ViewModel;
using SubSonic.Repository;

namespace HealthKick
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    private static Timer MyTimer;
    private static string logFilename;
    private static long previousSize;
    private readonly MediaEvent _mediaEvent = new MediaEvent();
    private StickerViewModel vm;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
      ShutdownMode = ShutdownMode.OnExplicitShutdown;

      //_mediaEvent.MediaWatcher += new MediaWatcherEventHandler(MeMediaWatcher);
      _mediaEvent.ContourUSBInserted += OnContourUSBInserted;
      // TODO Remove the logical device stuff
      _mediaEvent.Monitor("K:", _mediaEvent);

      //logFilename = GetUserDirectory() + @"\GLUCOFACTS Deluxe\logs\Barracuda.log";
      logFilename = @"C:\Bayer.db";

      var dao = new DAO();
      vm = new StickerViewModel(dao);
      vm.NewSticker();

      // Create system tray icon
      var tbi = new TaskbarIcon();
      tbi.Icon = new Icon("DefaultTrayIcon.ico");
      tbi.ToolTipText = "HealthKick!";
      tbi.ContextMenu = new ContextMenu();
      var mi = new MenuItem();
      mi.Header = "Exit";
      mi.Click += mi_Click;
      tbi.ContextMenu.Items.Add(mi);
      tbi.MenuActivation = PopupActivationMode.RightClick;

      // Timer for waiting after db accesses
      MyTimer = new Timer();
      MyTimer.Elapsed += MyTimerElapsed;
      MyTimer.Interval = 2000;
    }

    private void OnContourUSBInserted(object sender)
    {
      Console.WriteLine("Contour USB Inserted");
      vm.SetStatus("usb_inserted");
      WatchGlucofactsLog();
      if (!FocusIfMainWindowTitleIs("Bayer's GLUCOFACTS Deluxe Version V2.10"))
      {
        RunGlucofactsApplication(); 
      }
    }

    private void RunGlucofactsApplication()
    {
      string path = @"\Bayer HealthCare\GLUCOFACTS Deluxe\";
      string app = @"run.bat";
      var psi = new ProcessStartInfo(ProgramFilesx86() + path + app);
      psi.RedirectStandardOutput = true;
      psi.WindowStyle = ProcessWindowStyle.Hidden;
      psi.UseShellExecute = false;
      psi.WorkingDirectory = ProgramFilesx86() + path;
      Process glucofacts = Process.Start(psi);
      StreamReader myOutput = glucofacts.StandardOutput;
    }

    private void WatchGlucofactsLog()
    {
      var watch = new FileSystemWatcher();
      //watch.Path = GetUserDirectory() + @"\GLUCOFACTS Deluxe\logs\";
      watch.Path = @"C:\";
      watch.NotifyFilter = NotifyFilters.LastWrite;
      // Only watch log files.
      //watch.Filter = "Barracuda.log";
      watch.Filter = "Bayer.db";
      watch.Changed += OnChanged;
      watch.EnableRaisingEvents = true;
      // set existing size of file
      var fs = new FileStream(logFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      previousSize = fs.Length;
      fs.Close();
    }

    private static void OnChanged(object source, FileSystemEventArgs e)
    {
      Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
      // Reset timer
      MyTimer.Stop();
      MyTimer.Start();
    }

    // TODO Not used
    private static void ReadLog(object source, FileSystemEventArgs e)
    {
      using (FileStream fs = new FileStream(logFilename, FileMode.Open, FileAccess.Read, FileShare.None))
     {
       fs.Seek(previousSize, SeekOrigin.Begin);

       byte[] b = new byte[1024];
       UTF8Encoding temp = new UTF8Encoding(true);
       while (fs.Read(b, 0, b.Length) > 0)
       {
         Console.WriteLine(temp.GetString(b));
       }
       previousSize = fs.Length;
     }
    }

    private void MyTimerElapsed(object sender, ElapsedEventArgs e)
    {
      // read from db
      Console.WriteLine("Check database!");
      MyTimer.Stop();
      GetLatestResult();
    }

    private void GetLatestResult()
    {
      try
      {
        var connStr = @"Data Source=c:\\Bayer.db;Version=3;Read Only=True;";
        var conn = new SQLiteConnection(connStr);
        var cmd = new SQLiteCommand();
        cmd.Connection = conn;
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = "SELECT * FROM ResultData";
        cmd.Connection.Open();
        var reader = cmd.ExecuteReader();
        var dt = new DataTable();
        dt.Load(reader);
        foreach (DataColumn c in dt.Columns)
          Console.WriteLine(c.ColumnName);
        foreach(DataRow d in dt.Rows)
          Console.WriteLine(d);
        reader.Close();
        conn.Close();
      }
      catch (Exception e)
      {
        Console.WriteLine("EXCEPTION!\n" + e.StackTrace);
      }

      vm.SetStatus("good_results");
    }

    private static string GetUserDirectory()
    {
      return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private static string ProgramFilesx86()
    {
      if (8 == IntPtr.Size ||
          (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
      {
        return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
      }

      return Environment.GetEnvironmentVariable("ProgramFiles");
    }

    private void mi_Click(object sender, RoutedEventArgs e)
    {
      Shutdown();
    }

    private static void MeMediaWatcher(object sender, MediaEvent.DriveStatus driveStatus)
    {
      Console.WriteLine("Cool!" + driveStatus);
    }

    // Sets the window to be foreground
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    // Activate or minimize a window
    [DllImport("User32.DLL")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_SHOW = 5;
    private const int SW_MINIMIZE = 6;
    private const int SW_RESTORE = 9;

    // Focus a window if it exists
    public bool FocusIfMainWindowTitleIs(string name)
    {
      foreach (Process clsProcess in Process.GetProcesses())
      {
        if (clsProcess.MainWindowTitle.Contains(name))
        {
          ShowWindow(clsProcess.MainWindowHandle, SW_RESTORE);
          SetForegroundWindow(clsProcess.MainWindowHandle);
          return true;
        }
      }
      return false;
    }
  }
}