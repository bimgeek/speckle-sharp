﻿using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Alignment : Base
  {
    public List<ICurve> curves { get; set; } // a list of line, arc, and spirals, or polycurves comprised of the 3 curve types

    public string name { get; set; }

    public double startStation { get; set; }

    public double endStation { get; set; }

    /// <summary>
    /// Station equation list contains doubles indicating raw station back, station back, and station ahead for each station equation
    /// </summary>
    public List<double> stationEquations { get; set; }

    /// <summary>
    /// Station equation direction for the corresponding station equation should be true for increasing or false for decreasing
    /// </summary>
    public List<bool> stationEquationDirections { get; set; }

    public Polyline displayValue { get; set; }

    public string units { get; set; }

    public Alignment() { }

  }
}
