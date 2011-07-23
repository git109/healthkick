using System;
using System.IO;
using System.Management;

namespace HealthKick.DeviceMonitor
{
  /// <summary>Media watcher delegate.</summary>
  /// <param name="sender"></param>
  /// <param name="driveStatus"></param>
  public delegate void MediaWatcherEventHandler(object sender, MediaEvent.DriveStatus driveStatus);

  public delegate void ContourUSBInserted(object sender);

  /// <summary>Class to monitor devices.</summary>
  public class MediaEvent
  {
    #region Variables

    private string m_logicalDrive;
    private ManagementEventWatcher m_managementEventWatcher;

    #endregion

    #region Events

    public event MediaWatcherEventHandler MediaWatcher;

    public event ContourUSBInserted ContourUSBInserted;

    #endregion

    #region Enums

    #region DriveStatus enum

    /// <summary>The drive status.</summary>
    public enum DriveStatus
    {
      Unknown = -1,
      Ejected = 0,
      Inserted = 1,
    }

    #endregion

    #region DriveType enum

    /// <summary>The drive types.</summary>
    public enum DriveType
    {
      Unknown = 0,
      NoRootDirectory = 1,
      RemoveableDisk = 2,
      LocalDisk = 3,
      NetworkDrive = 4,
      CompactDisk = 5,
      RamDisk = 6
    }

    #endregion

    #endregion

    #region Monitoring

    /// <summary>Starts the monitoring of device.</summary>
    public void Monitor(string path, MediaEvent mediaEvent)
    {
      if (null == mediaEvent)
      {
        throw new ArgumentException("Media event cannot be null!");
      }

      Exit(); //In case same class was called make sure only one instance is running
      m_logicalDrive = GetLogicalDrive(path); //Keep logical drive to check
      WqlEventQuery wql;
      var observer = new ManagementOperationObserver(); //Bind to local machine
      var opt = new ConnectionOptions(); //Sets required privilege
      opt.EnablePrivileges = true;
      var scope = new ManagementScope("root\\CIMV2", opt);

      try
      {
        wql = new WqlEventQuery();
        wql.EventClassName = "__InstanceCreationEvent";
        wql.WithinInterval = new TimeSpan(0, 0, 1);
        //wql.Condition = String.Format(@"TargetInstance ISA 'Win32_LogicalDisk' and TargetInstance.DeviceID = '{0}'", this.m_logicalDrive);
        wql.Condition =
          String.Format(
            @"TargetInstance ISA 'Win32_PnPEntity' and TargetInstance.Name = 'CONTOUR USB'");
        m_managementEventWatcher = new ManagementEventWatcher(scope, wql);
        //Register async. event handler
        m_managementEventWatcher.EventArrived += mediaEvent.MediaEventArrived;
        m_managementEventWatcher.Start();
      }
      catch (Exception e)
      {
        Exit();
        throw new Exception("Media Check: " + e.Message);
      }
    }

    /// <summary>Stops the monitoring of device.</summary>
    public void Exit()
    {
      //In case same class was called make sure only one instance is running
      /////////////////////////////////////////////////////////////
      if (null != m_managementEventWatcher)
      {
        try
        {
          m_managementEventWatcher.Stop();
          m_managementEventWatcher = null;
        }
        catch
        {
        }
      }
    }

    #endregion

    #region Helpers

    private DriveStatus m_driveStatus = DriveStatus.Unknown;

    /// <summary>Triggers the event when change on device occured.</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MediaEventArrived(object sender, EventArrivedEventArgs e)
    {
      //PrintAllProperties(sender, e);
      //GetDriveStatus(sender, e);
      PrintInsertedStatus(sender, e);
    }

    private void PrintInsertedStatus(object sender, EventArrivedEventArgs e)
    {
      PropertyData pd = e.NewEvent.Properties["TargetInstance"];
      if (pd != null)
      {
        var mbo = pd.Value as ManagementBaseObject;
        if ((string) mbo.Properties["Name"].Value == "CONTOUR USB")
        {
          ContourUSBInserted(this); // Notify listeners that the Contour device was inserted
          //Console.WriteLine(mbo.Properties["DeviceID"].Value);
          //Console.WriteLine(mbo.Properties["VendorID"].Value);
        }
      }
    }

    private void PrintAllProperties(object sender, EventArrivedEventArgs e)
    {
      const string eventCaptured = "\n===EVENT CAPTURED===";
      const string eventProperties = "\n---PROPERTIES---";
      const string eventCapturedEnd = "\n====================";
      
      Console.WriteLine(eventCaptured);
      foreach (PropertyData pd in e.NewEvent.Properties)
      {
        Console.WriteLine("{0},{1},{2},{3}", pd.Name, pd.Type, pd.Value, pd.Origin);

        ManagementBaseObject mbo = null;
        if ((mbo = pd.Value as ManagementBaseObject) != null)
        {
          Console.WriteLine(eventProperties);
          foreach (PropertyData prop in mbo.Properties) Console.WriteLine("{0} - {1}", prop.Name, prop.Value);
        }
      }
      Console.WriteLine(eventCapturedEnd);
    }

    private void GetDriveStatus(object sender, EventArrivedEventArgs e)
    {
      // Get the Event object and display it
      PropertyData pd = e.NewEvent.Properties["TargetInstance"];
      DriveStatus driveStatus = m_driveStatus;

      if (pd != null)
      {
        var mbo = pd.Value as ManagementBaseObject;
        var info = new DriveInfo((string) mbo.Properties["DeviceID"].Value);
        driveStatus = info.IsReady ? DriveStatus.Inserted : DriveStatus.Ejected;
      }

      if (driveStatus != m_driveStatus)
      {
        m_driveStatus = driveStatus;
        if (null != MediaWatcher)
        {
          MediaWatcher(sender, driveStatus);
        }
      }
    }

    /// <summary>Gets the logical drive of a given path.</summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private string GetLogicalDrive(string path)
    {
      var dirInfo = new DirectoryInfo(path);
      string root = dirInfo.Root.FullName;
      string logicalDrive = root.Remove(root.IndexOf(Path.DirectorySeparatorChar));
      return logicalDrive;
    }

    #endregion
  }
}