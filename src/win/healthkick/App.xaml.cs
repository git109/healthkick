using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using HealthKick.DeviceMonitor;
using HealthKick.ViewModel;
using Timer = System.Timers.Timer;

namespace HealthKick
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    private const int SW_SHOW = 5;
    private const int SW_MINIMIZE = 6;
    private const int SW_RESTORE = 9;
    private static Timer MyTimer;
    private static string dbPath;
    private static string dbFilename;
    private static string dbPathAndFilename;
    private static long previousSize;
    private readonly MediaEvent _mediaEvent = new MediaEvent();
    private long _lastSequenceNumber = -1;
    private StickerViewModel vm;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
      ShutdownMode = ShutdownMode.OnExplicitShutdown;

      //_mediaEvent.MediaWatcher += new MediaWatcherEventHandler(MeMediaWatcher);
      _mediaEvent.ContourUSBInserted += OnContourUSBInserted;
      // TODO Remove the logical device stuff
      _mediaEvent.Monitor("K:", _mediaEvent);

      //dbPath = GetUserDirectory() + @"\GLUCOFACTS Deluxe\logs\Barracuda.log";
      // TODO rename
      //dbPath = @"C:\Bayer.db";
      // NOTE: For debugging copy Bayer.db to the Debug output directory
      dbPath = Directory.GetCurrentDirectory();
      dbFilename = "Bayer.db";
      dbPathAndFilename = dbPath  + @"\" + dbFilename;

      var dao = new DAO();
      vm = new StickerViewModel(dao);
      //vm.NewSticker();

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

      // Create a sticker to show progress if there isn't one already
      if (vm.Windows.Count == 0)
      {
        vm.NewStickerThreadSafeDelegate();
      }
      vm.SetStatus(StickerStatus.DownloadingReading);
      _lastSequenceNumber = GetLatestSequenceNumber();
      vm.LastReading = GetLatestReadingTime();
      WatchGlucofactsLog(); // TODO deprecated?
      if (!FocusIfMainWindowTitleIs("Bayer's GLUCOFACTS Deluxe Version V2.10"))
      {
        RunGlucofactsApplication();
      }
    }

    private long GetLatestSequenceNumber()
    {
      DataTable dt = GetLatestResult();
      if (dt == null) return -1;
      object val = dt.Rows[0]["Sequence_Number"];
      return Convert.ToInt64(val);
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
      watch.Path = dbPath;
      watch.NotifyFilter = NotifyFilters.LastWrite;
      // Only watch log files.
      //watch.Filter = "Barracuda.log";
      watch.Filter = dbFilename;
      watch.Changed += OnChanged;
      watch.EnableRaisingEvents = true;
      // set existing size of file
      var fs = new FileStream(dbPathAndFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
      using (var fs = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.None))
      {
        fs.Seek(previousSize, SeekOrigin.Begin);

        var b = new byte[1024];
        var temp = new UTF8Encoding(true);
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
      CheckLatestResult();
    }

    private static DataTable GetLatestResult()
    {
      try
      {
        string connStr = @"Data Source=" + dbPathAndFilename + ";Version=3;Read Only=True;";
        var conn = new SQLiteConnection(connStr);
        var cmd = new SQLiteCommand();
        cmd.Connection = conn;
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = "SELECT * FROM ResultData ORDER BY Sequence_Number DESC ";
        cmd.Connection.Open();
        SQLiteDataReader reader = cmd.ExecuteReader();
        var dt = new DataTable();
        dt.Load(reader);
        foreach (DataColumn c in dt.Columns) Console.WriteLine(c.ColumnName);
        foreach (DataRow d in dt.Rows) Console.WriteLine(d["Creation_Time"]);
        reader.Close();
        conn.Close();
        return dt;
      }
      catch (Exception e)
      {
        Console.WriteLine("EXCEPTION!\n" + e.StackTrace);
      }
      return null;
    }

    public static DateTime GetLatestReadingTime()
    {
      var dt = GetLatestResult();
      if (dt == null)
      {
        //vm.SetStatus(StickerStatus.Error);
        // TODO better error handling
        return new DateTime(1970, 1, 1);
      }
      // TODO add results to display
      string time = dt.Rows[0]["Test_Time"] as string;
      object date = dt.Rows[0]["Test_Date"];
      DateTime parsedDate;
      DateTime parsedTime;
      long longDate = Convert.ToInt64(date);
      parsedDate = FromUnixTime(longDate);
      DateTime.TryParse(time, out parsedTime);
      DateTime resultDate = parsedDate.Date + parsedTime.TimeOfDay;

      return resultDate;
    }

    public static DateTime FromUnixTime(long unixTime)
    {
      var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
      return epoch.AddMilliseconds(unixTime);
    }

    private void CheckLatestResult()
    {
      DataTable dt = GetLatestResult();

      if (dt == null)
      {
        vm.SetStatus(StickerStatus.Error);
        return;
      }

      // TODO add results to display
      if (Convert.ToInt64(dt.Rows[0]["Sequence_Number"]) > _lastSequenceNumber)
      {
        // there has been a new reading
        double reading = (Convert.ToInt64(dt.Rows[0]["Measurement_Value"])*0.0555);
        var d = new decimal(reading);
        vm.LastReadingValue = (double) Decimal.Round(d, 1);
        vm.SetStatus(StickerStatus.DisplayingReading);
      }
      else
      {
        // no new readings
        vm.SetStatus(StickerStatus.NoNewReading);
      }
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
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    // Activate or minimize a window
    [DllImport("User32.DLL")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

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