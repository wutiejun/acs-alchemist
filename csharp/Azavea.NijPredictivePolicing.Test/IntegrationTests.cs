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
using NUnit.Framework;
using log4net;
using Azavea.NijPredictivePolicing.Test.Helpers;
using System.IO;
using Azavea.NijPredictivePolicing.ACSAlchemist;

namespace Azavea.NijPredictivePolicing.Test
{
    [TestFixture]
    public class IntegrationTests
    {
        private static ILog _log = null;

        /// <summary>
        /// Place to dump files generated by tests
        /// </summary>
        protected const string OutputDir = "output\\";

        [TestFixtureSetUp]
        public void Init()
        {
            _log = LogHelpers.ResetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            if (!Directory.Exists(OutputDir))
                Directory.CreateDirectory(OutputDir);
        }

        [Test]
        public void FullImportTest()
        {
            

            var myVariablesStr = @"
B01001001,TOTALPOP
B01001002,TOTALMALE
B01001026,TOTALFEMALE
";
            File.WriteAllText("myVariablesFile.txt", myVariablesStr);

            var args = new string[] {
                "-s",
                "Wyoming",
                "-e",
                "150",
                "-v",
                "myVariablesFile.txt",
                "-jobName",
                "Test01",
                "–exportToShape"
            };

            _log.Debug("Starting Sample Import / Export");
            Program.Main(args);
            

            Assert.IsTrue(File.Exists("Test01.shp"), "Shapefile not generated!");
        }






    }
}
