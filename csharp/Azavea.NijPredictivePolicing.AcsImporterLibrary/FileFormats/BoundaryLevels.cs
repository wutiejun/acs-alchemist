﻿/*
  Copyright (c) 2012 Azavea, Inc.
 
  This file is part of ACS Alchemist.

  ACS Alchemist is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  ACS Alchemist is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with ACS Alchemist.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Azavea.NijPredictivePolicing.ACSAlchemistLibrary.FileFormats
{
    /// <summary>
    /// Describes the supported census boundary levels
    /// See http://www.census.gov/geo/maps-data/data/summary_level.html
    /// </summary>
    public enum BoundaryLevels
    {
        None = 0,
       
        //[DescriptionAttribute("Census Regions")]
        //census_regions = 20,

        //[DescriptionAttribute("Census Divisions")]
        //census_divisions = 30,

        //[DescriptionAttribute("State and State Equivalent Areas")]
        //states = 40,

        //[DescriptionAttribute("County and County Equivalent Areas by State")]
        //counties = 50,

        //[DescriptionAttribute("County Subdivisions by State 2000 ")]
        //county_subdivisions = 60,

        [DescriptionAttribute("Census Tracts by State 2000 ")]
        census_tracts = 140,

        [DescriptionAttribute("Census Block Groups by State 2000 ")]
        census_blockgroups = 150,

        /*
        [DescriptionAttribute("Voting Districts by State 2000 ")]
        voting = 700,

        [DescriptionAttribute("3-Digit ZIP Code Tabulation Areas (ZCTAs) 2000 by State")]
        zipthree = -1,

        [DescriptionAttribute("5-Digit ZIP Code Tabulation Areas (ZCTAs) 2000 by State")]
        zipfive = -2
         */

    }
}
