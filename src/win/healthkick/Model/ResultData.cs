// -----------------------------------------------------------------------
// <copyright file="ResultData.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace healthkick.Model
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  /// <summary>
  /// TODO: Update summary.
  /// </summary>
  public class ResultData
  {
    public Guid PersonGUID { get; set; }
    public long CreationTime { get; set; }
    public long TestDate { get; set; }
    public long UserMarks { get; set; }
    public long MeasurementValue { get; set; }
  }
}

