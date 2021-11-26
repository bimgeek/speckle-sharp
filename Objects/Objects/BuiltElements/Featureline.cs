﻿using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.BuiltElements
{
  public class Featureline : Base
  {
    public ICurve baseCurve { get; set; }

    public string name { get; set; } // this is the point code in C3D

    public string units { get; set; }

    public Featureline() { }

  }
}
