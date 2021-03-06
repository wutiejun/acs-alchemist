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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using log4net.Appender;
using log4net.Layout;
using log4net;
using Azavea.NijPredictivePolicing.Common;
using Azavea.NijPredictivePolicing.ACSAlchemistLibrary;
using Azavea.NijPredictivePolicing.ACSAlchemist;
using System.IO;
using Azavea.NijPredictivePolicing.ACSAlchemistLibrary.FileFormats;
using System.Reflection;
using System.Drawing.Text;

namespace Azavea.NijPredictivePolicing.AcsAlchemistGui
{
    public partial class MainForm : Form
    {
        private static ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the controls and whatnot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                ShowLoadingSpinner();

                //TODO: special initializer for the logger / or something to get the output stream before the copyright / etc is shown

                var appender = new TextboxAppender(this.txtLogConsole);
                FormController.Instance.InitLogging(appender);

                //wait until after the form is loaded to do this, we want to show the log output on the form
                FormController.Instance.Initialize();

                //
                // Initialize the rest of the form
                //

                this.LoadFonts();
                this.PopulateLists();
                this.AddDefaultTooltips();
                this.SmartToggler();

                this.FixWeirdStyles();

                this.MinimumSize = new Size(800, 650);

                this.PopulateControls();
            }
            catch (Exception ex)
            {
                this.DisplayException("Form Load", ex);
            }
            finally
            {
                HideLoadingSpinner();
            }
        }

        /// <summary>
        /// Windows configurations differ in the actual colors used for different elements 'InactiveText', 'BackgroundColor', etc.
        /// Our log text is a little 'grayed out' to be less distracting, but on some configurations this can make it invisible.
        /// </summary>
        protected void FixWeirdStyles()
        {
            //fix for windows with weird styles
            if (this.txtLogConsole.ForeColor.ToArgb() == this.txtLogConsole.BackColor.ToArgb())
            {
                //this happens on some windows configurations:
                this.txtLogConsole.ForeColor = System.Drawing.SystemColors.WindowText;
            }
        }

        /// <summary>
        /// We're doing this because fonts are rarely consistent between machines
        /// (in this case, my dev laptop, and my dev workstation).  This changes the
        /// appearance of the console log, and the controls quite significantly (stuff disappears),
        /// so it's pretty important that the font is right.
        /// </summary>
        protected void LoadFonts()
        {
            try
            {
                //copied from http://dotnet-coding-helpercs.blogspot.com/
                unsafe
                {
                    var fonts = new List<byte[]>();
                    fonts.Add(Resource1.LiberationMono_Regular);
                    fonts.Add(Resource1.LiberationSans_Regular);

                    foreach (var buffer in fonts)
                    {
                        fixed (byte* pFontData = buffer)
                        {
                            uint dummy = 0;
                            _fontCollection.AddMemoryFont((IntPtr)pFontData, buffer.Length);
                            AddFontMemResourceEx((IntPtr)pFontData, (uint)buffer.Length, IntPtr.Zero, ref dummy);
                        }
                    }
                }

                //opting to leave the form alone for the moment, because these fonts aren't especially awesome
                //this.Font =  new Font(_fontCollection.Families[1], 9);

                //however, we absolutely need a monospaced font here for this to look correct.
                txtLogConsole.Font = new Font(_fontCollection.Families[0], 8f);
            }
            catch (Exception ex)
            {
                this.DisplayException("Error Loading Embedded Font", ex);
            }
        }

        #region Fancy Font Stuff

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont,
        IntPtr pdv, [System.Runtime.InteropServices.In] ref uint pcFonts);
        private PrivateFontCollection _fontCollection = new PrivateFontCollection();

        #endregion Fancy Font Stuff





        /// <summary>
        /// Populates our 'year', 'state', 'summary level', and 'srid' controls, 
        /// as well as any other "choose from a set" controls that come up.
        /// </summary>
        protected void PopulateLists()
        {
            this.cboYear.DataSource = new BindingSource(FormController.Instance.AvailableYears, string.Empty);
            this.cboStates.DataSource = new BindingSource(FormController.Instance.AvailableStates, string.Empty);
            this.cboSummaryLevel.DataSource = new BindingSource(FormController.Instance.AvailableLevels, string.Empty);


            this.cboStates.FormattingEnabled = true;
            this.cboStates.Format += delegate(object sender, ListControlConvertEventArgs e)
            {
                AcsState state = (AcsState)e.Value;
                if (state == AcsState.None) { e.Value = string.Empty; }
                else { e.Value = state.ToString(); }
            };

            this.cboSummaryLevel.FormattingEnabled = true;
            this.cboSummaryLevel.Format += delegate(object sender, ListControlConvertEventArgs e)
            {
                BoundaryLevels level = (BoundaryLevels)e.Value;
                switch (level)
                {
                    case BoundaryLevels.None:
                        e.Value = string.Empty;
                        break;
                    //case BoundaryLevels.counties:
                    //    e.Value = "Counties";
                    //    break;
                    //case BoundaryLevels.county_subdivisions:
                    //    e.Value = "County Subdivisions";
                    //    break;
                    case BoundaryLevels.census_tracts:
                        e.Value = "Census Tracts";
                        break;
                    case BoundaryLevels.census_blockgroups:
                        e.Value = "Census Blockgroups";
                        break;
                }
            };

            //NOTE! We're doing this initialization in "radioSRIDFromList_CheckedChanged",
            //instead of here, this is because it's expensive (we have to read through a whole file)
            //so we're shaving a half-second off the init, and moving it to a spot where the user shouldn't notice.
            //this.cboProjections.DataSource = new BindingSource(FormController.Instance.AvailableProjections, string.Empty);                        
        }


        /// <summary>
        /// collection of tooltip controls
        /// </summary>
        protected Dictionary<Control, ToolTip> _tooltips = new Dictionary<Control, ToolTip>();

        /// <summary>
        /// Helper for setting tooltips
        /// </summary>
        /// <param name="ctl"></param>
        /// <param name="label"></param>
        protected void SetTooltip(Control ctl, string label)
        {
            if (!this._tooltips.ContainsKey(ctl))
            {
                this._tooltips[ctl] = new ToolTip();
            }
            this._tooltips[ctl].SetToolTip(ctl, label);
        }


        protected void AddDefaultTooltips()
        {
            this.SetTooltip(chkPreserveJamValues, "When checked, does not attempt to force all error values to be numeric");
            this.SetTooltip(chkStripExtraGeoID, "When checked, it adds a copy of the GEOID column \"GEOID_STRP\" except without the \"15000US\" prefix ");
            this.SetTooltip(cboIncludeEmptyGeom, "When checked, it keeps all cells or polygons in the output, even if they don't have any data ");

            this.SetTooltip(txtFishnetCellSize, "These units MUST match the currently selected projection units (meters, degrees, feet, etc)");
            this.SetTooltip(lblGridCellUnits, "These units MUST match the currently selected projection units (meters, degrees, feet, etc)");
        }



        #region Menu Events


        /// <summary>
        /// Returns false if the following action should be cancelled
        /// </summary>
        /// <returns></returns>
        public bool CheckSave()
        {
            if (this.IsDirty)
            {
                //show prompt
                var resp = MessageBox.Show("Save job first?", "Save your settings as a job file?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (resp == System.Windows.Forms.DialogResult.Yes)
                {
                    //do a save
                    this.saveJobFileToolStripMenuItem_Click(null, null);

                    /*
                     * Note! we don't do the infinite re-ask loop here.  But 
                     */
                    return true;
                }
                else if (resp == System.Windows.Forms.DialogResult.No)
                {
                    //continue
                    return true;
                }
                else if (resp == System.Windows.Forms.DialogResult.Cancel)
                {
                    //bail
                    return false;
                }
                //if prompt == cancel, return false;
            }
            return true;
        }


        private void openJobFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckSave()) { return; }

            if (openFileJob.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtJobFilePath.Text = openFileJob.FileName;

                //change the current working directory to the folder where the job file was
                //so that relative paths work as expected
                Environment.CurrentDirectory = Path.GetDirectoryName(txtJobFilePath.Text);

                FormController.Instance.LoadNewJobInstance(txtJobFilePath.Text);
                this.PopulateControls();
                this.ValidateChildren();
            }
        }

        private void newJobToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckSave()) { return; }

            FormController.Instance.NewDefaultJobInstance();
            this.PopulateControls();
        }



        private void saveJobFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileJob.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(saveFileJob.FileName))
                {
                    MessageBox.Show("No file provided", "Unable to save job file");
                    return;
                }

                // if it's not cancelled
                txtJobFilePath.Text = saveFileJob.FileName;
                this.GatherInputs(true);        //gather all the settings
                FormController.Instance.JobInstance.SaveJobFile(txtJobFilePath.Text);
            }
        }


        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckSave()) { return; }

            this.Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (AboutBox1 about = new AboutBox1())
            {
                about.ShowDialog();
            }
        }


        /// <summary>
        /// TODO: This is not yet implemented, providing a helpful error message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveMessageLog_Click(object sender, EventArgs e)
        {
            txtMessageLogFilePath.Text = Path.Combine(FileUtilities.GetApplicationPath(), "logs");

            _log.Debug("Redirecting Log output to a specified file is not yet supported by this interface.");
            _log.DebugFormat("You can find a copy of the logs here: {0}", txtMessageLogFilePath.Text);

            //saveFileMessageLog.ShowDialog();
            //txtMessageLogFilePath.Text = saveFileMessageLog.FileName;
        }


        #endregion Menu Events


        #region File Browsers

        /// <summary>
        /// Opens a file dialog for -- Variables File
        /// Extensions: *.txt, *.vars / All
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowseVariableFile_Click(object sender, EventArgs e)
        {
            if (ofdVariablesFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtVariableFilePath.Text = ofdVariablesFile.FileName;
                txtVariableFilePath.Focus();
            }
        }

        /// <summary>
        /// Browse for the "Output Folder"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowseOutputFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserOutputDir.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtOutputDirectory.Text = folderBrowserOutputDir.SelectedPath;
            }
        }

        /// <summary>
        /// Browse for "Grid boundary-alignment file"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowseFishnetEnvelopeFile_Click(object sender, EventArgs e)
        {
            if (ofdGridEnvelopeShp.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtFishnetEnvelopeFilePath.Text = ofdGridEnvelopeShp.FileName;
            }
        }

        /// <summary>
        /// Browse for the "Output Projection File"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowsePrjFile_Click(object sender, EventArgs e)
        {
            if (ofdOutputProjection.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPrjFilePath.Text = ofdOutputProjection.FileName;
            }
        }

        /// <summary>
        /// Browse for the "Output Filter Boundary" Shapefile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowseBoundaryShpFile_Click(object sender, EventArgs e)
        {
            if (ofdExportBoundaryShp.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtBoundaryShpFilePath.Text = ofdExportBoundaryShp.FileName;
            }
        }

        private void btnBrowseWorking_Click(object sender, EventArgs e)
        {
            if (fbdWorkingDir.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtWorkingDirectory.Text = fbdWorkingDir.SelectedPath;
            }
        }

        #endregion File Browsers


        #region Event Boilerplate

        /// <summary>
        /// Perform all our form enable/disables in one place, so the form is always in a consistent state
        /// (this also makes this much easier to maintain)
        /// </summary>
        public void SmartToggler()
        {
            /** TODO: Anything that enables/disables */


            //if (radioDefaultSRID.Checked) { }     //nothing to do here

            //projection list
            cboProjections.Enabled = (radioSRIDFromList.Checked && !backgroundWorker1.IsBusy);

            //projection file controls
            txtPrjFilePath.Enabled = (radioSRIDFile.Checked && !backgroundWorker1.IsBusy);
            btnBrowsePrjFile.Enabled = (radioSRIDFile.Checked && !backgroundWorker1.IsBusy);

            {
                bool enabledIfIdle = !backgroundWorker1.IsBusy;

                this.btnShapefile.Enabled = enabledIfIdle;
                this.btnFishnet.Enabled = enabledIfIdle;

                this.cboYear.Enabled = enabledIfIdle;
                this.cboStates.Enabled = enabledIfIdle;
                this.cboSummaryLevel.Enabled = enabledIfIdle;
                this.txtVariableFilePath.Enabled = enabledIfIdle;
                this.txtOutputDirectory.Enabled = enabledIfIdle;
                this.txtWorkingDirectory.Enabled = enabledIfIdle;

                this.radioDefaultSRID.Enabled = enabledIfIdle;
                this.radioSRIDFromList.Enabled = enabledIfIdle;
                this.radioSRIDFile.Enabled = enabledIfIdle;


                this.txtJobName.Enabled = enabledIfIdle;

                this.chkReplaceJob.Enabled = enabledIfIdle;
                this.txtFishnetCellSize.Enabled = enabledIfIdle;
                this.txtFishnetEnvelopeFilePath.Enabled = enabledIfIdle;
                this.cboIncludeEmptyGeom.Enabled = enabledIfIdle;
                this.txtBoundaryShpFilePath.Enabled = enabledIfIdle;
                this.chkPreserveJamValues.Enabled = enabledIfIdle;
                this.chkStripExtraGeoID.Enabled = enabledIfIdle;



                this.btnBrowseVariableFile.Enabled = enabledIfIdle;
                this.btnBrowseOutputFolder.Enabled = enabledIfIdle;
                this.btnBrowseWorking.Enabled = enabledIfIdle;

                this.btnBrowseBoundaryShpFile.Enabled = enabledIfIdle;
                this.btnBrowseFishnetEnvelopeFile.Enabled = enabledIfIdle;
            }
        }

        private void radioDefaultSRID_CheckedChanged(object sender, EventArgs e)
        {
            SmartToggler();
        }

        private void radioSRIDFromList_CheckedChanged(object sender, EventArgs e)
        {
            SmartToggler();

            ShowLoadingSpinner();
            //we're not doing this on startup, because it's expensive / slow,
            //so lets only do it when they ask us to:

            //if we haven't yet, populate the SRID dropdown:
            if (this.cboProjections.Items.Count == 0)
            {
                this.cboProjections.DataSource = new BindingSource(FormController.Instance.AvailableProjections, string.Empty);
            }

            HideLoadingSpinner();
        }

        private void radioSRIDFile_CheckedChanged(object sender, EventArgs e)
        {
            SmartToggler();
        }

        /// <summary>
        /// Sets the projection text as the tooltip, best way we have for showing what projection they've selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cboProjections_SelectedIndexChanged(object sender, EventArgs e)
        {
            //NOTE: Normally, I would optimize this call, but it seems incredibly fast on my machine,
            //please let me know if it seems slow, and I'll just read it into a dictionary and use that instead.
            var projectionText = Utilities.GetCoordinateSystemWKTByID(this.cboProjections.Text);

            this.SetTooltip(this.radioSRIDFromList, projectionText);
            this.SetTooltip(this.cboProjections, projectionText);
        }


        private void btnCancel_Click(object sender, EventArgs e)
        {
            _log.Debug("Cancelling... (this might take a minute)");
            FormController.Instance.JobInstance.Cancel();
        }

        #endregion Event Boilerplate







        /// <summary>
        /// A very basic error display helper --
        ///  NOTE! It is up to the developer to choose what exceptions are FATAL or not.
        /// This helper is for potentially NON-FATAL exceptions / or those that can be recovered from
        /// </summary>
        /// <param name="label"></param>
        /// <param name="ex"></param>
        protected void DisplayException(string label, Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("An exception was caught during \"{0}\"{1}", label, Environment.NewLine);
            sb.AppendFormat("Message:{0}{1}{1}", ex.Message, Environment.NewLine);

            sb.Append("The program might not continue to run as expected, please restart the application");

            MessageBox.Show(sb.ToString(), "An exception was caught");
        }



        protected void ShowLoadingSpinner()
        {
            this.pgbStatus.Style = ProgressBarStyle.Continuous;
        }

        protected void HideLoadingSpinner()
        {
            this.pgbStatus.Style = ProgressBarStyle.Blocks;
        }



        /// <summary>
        /// Copies our form onto a 'job instance'
        /// </summary>
        /// <param name="isFishnet"></param>
        protected void GatherInputs(bool isFishnet)
        {
            /** TODO: Gather all inputs and update our controller / job instance */

            var importObj = FormController.Instance.JobInstance;

            FormController.Instance.JobInstance.WorkOffline = FormController.Instance.IsOffline;

            importObj.Year = cboYear.Text;                                             //1
            importObj.State = (AcsState)cboStates.SelectedValue;                       //2
            importObj.SummaryLevel = ((int)cboSummaryLevel.SelectedValue).ToString();  //3
            importObj.IncludedVariableFile = txtVariableFilePath.Text;                 //4
            importObj.OutputFolder = txtOutputDirectory.Text;                          //5
            importObj.WorkingFolder = txtWorkingDirectory.Text;                        //6

            //TODO: Stub in default output folder?

            //srid
            if (radioDefaultSRID.Checked)
            {
                importObj.OutputProjection = string.Empty;
            }
            if (radioSRIDFromList.Checked)
            {
                importObj.OutputProjection = this.cboProjections.Text;
            }
            if (radioSRIDFile.Checked)
            {
                importObj.OutputProjection = this.txtPrjFilePath.Text;
            }


            //stub in the default jobname
            if (string.IsNullOrEmpty(txtJobName.Text))
            {
                txtJobName.Text = string.Format("{0}_{1}_{2}", importObj.Year, importObj.State, DateTime.Now.ToShortDateString().Replace('/', '_'));
                _log.DebugFormat("Jobname was empty, using {0}", txtJobName.Text);
            }


            //job name:
            importObj.JobName = txtJobName.Text;
            importObj.ReusePreviousJobTable = (chkReplaceJob.Checked) ? string.Empty : "true";

            //shapefile:
            importObj.ExportToShapefile = (!isFishnet) ? "true" : string.Empty;

            //fishnet:
            importObj.ExportToGrid = (isFishnet) ? txtFishnetCellSize.Text : string.Empty;
            importObj.GridEnvelope = (isFishnet) ? txtFishnetEnvelopeFilePath.Text : string.Empty;

            //this applies to both shapefile and fishnet exports (despite its name)
            importObj.IncludeEmptyGridCells = (cboIncludeEmptyGeom.Checked) ? "true" : string.Empty;

            //clip bounds
            importObj.ExportFilterShapefile = txtBoundaryShpFilePath.Text;

            //optional flags
            importObj.PreserveJam = (chkPreserveJamValues.Checked) ? "true" : string.Empty;
            importObj.AddStrippedGEOIDcolumn = (chkStripExtraGeoID.Checked) ? "true" : string.Empty;
            importObj.AddGeometryAttributesToOutput = "true";//TODO: AddGeometryAttributesToOutput (Default is true)
            

            this.IsDirty = false;
        }

        /// <summary>
        /// Copies a 'job instance' onto our form
        /// </summary>
        protected void PopulateControls()
        {
            this.errorProvider1.Clear();


            var importObj = FormController.Instance.JobInstance;

            cboYear.Text = importObj.Year;                                          //1
            cboStates.SelectedItem = importObj.State;                               //2
            cboSummaryLevel.SelectedItem = Utilities.GetAs<BoundaryLevels>(importObj.SummaryLevel, BoundaryLevels.None);   //3
            txtVariableFilePath.Text = (importObj.IncludedVariableFile + string.Empty).Trim('\"');   //4
            txtOutputDirectory.Text = importObj.OutputFolder;                       //5
            txtWorkingDirectory.Text = importObj.WorkingFolder;                     //6

            if (string.IsNullOrEmpty(importObj.OutputProjection))
            {
                radioDefaultSRID.Checked = true;
            }
            else if (File.Exists(importObj.OutputProjection))
            {
                radioSRIDFile.Checked = true;
                this.txtPrjFilePath.Text = importObj.OutputProjection;
            }
            else
            {
                radioSRIDFromList.Checked = true;
                this.cboProjections.Text = importObj.OutputProjection;
            }


            ////job name:
            txtJobName.Text = importObj.JobName;
            chkReplaceJob.Checked = (string.IsNullOrEmpty(importObj.ReusePreviousJobTable));

            //nothing to do here
            //importObj.ExportToShapefile = (!isFishnet) ? "true" : string.Empty;

            //fishnet:
            txtFishnetCellSize.Text = importObj.ExportToGrid;
            txtFishnetEnvelopeFilePath.Text = importObj.GridEnvelope;

            //this applies to both shapefile and fishnet exports (despite its name)
            cboIncludeEmptyGeom.Checked = (!string.IsNullOrEmpty(importObj.IncludeEmptyGridCells));

            //clip bounds
            txtBoundaryShpFilePath.Text = importObj.ExportFilterShapefile;

            //optional flags
            chkPreserveJamValues.Checked = (!string.IsNullOrEmpty(importObj.PreserveJam));
            chkStripExtraGeoID.Checked = (!string.IsNullOrEmpty(importObj.AddStrippedGEOIDcolumn));
            //TODO: AddGeometryAttributesToOutput (Default is true)

            //reset the progress bar
            pgbStatus.Value = 0;

            this.IsDirty = false;
        }


        /// <summary>
        /// Sanity check to determine if we have enough information to start a job / export --
        /// 
        /// This function should display a MessageBox if something is really wrong
        /// </summary>
        /// <param name="isFishnet"></param>
        protected bool CheckValidity(bool isFishnet)
        {
            /** TODO: Gather all inputs and update our controller / job instance */

            var importObj = FormController.Instance.JobInstance;

            if (string.IsNullOrEmpty(importObj.Year))
            {
                MessageBox.Show("No year selected", "Required setting missing", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }

            if (importObj.State == AcsState.None)
            {
                MessageBox.Show("No state selected", "Required setting missing", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }

            if (string.IsNullOrEmpty(importObj.SummaryLevel))
            {
                MessageBox.Show("No summary level selected", "Required setting missing", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }

            if (string.IsNullOrEmpty(importObj.IncludedVariableFile))
            {
                MessageBox.Show("No variables file selected", "Required setting missing", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }

            return true;
        }


        protected void TryRunExport(bool isFishnet)
        {
            GatherInputs(isFishnet);
            if (!CheckValidity(isFishnet))
            {
                return;
            }

            _log.Debug("Ready to go!");
            HideLoadingSpinner();
            this.backgroundWorker1.RunWorkerAsync(FormController.Instance.JobInstance);

            SmartToggler();
        }


        /// <summary>
        /// Export a shapefile using census boundaries!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnShapefile_Click(object sender, EventArgs e)
        {
            TryRunExport(false);
        }

        /// <summary>
        /// Export a fishnet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFishnet_Click(object sender, EventArgs e)
        {
            TryRunExport(true);
        }


        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            ImportJob job = (ImportJob)e.Argument;
            job.OnProgressUpdated += new ImportJob.ProgressUpdateHandler(this.backgroundWorker1.ReportProgress);
            e.Result = job.ExecuteJob();


            //EXTRA CREDIT: add support for cancellation
        }

        /// <summary>
        /// On progress updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.pgbStatus.Value = e.ProgressPercentage;
            //this.pgbStatus
            //string message = (string)e.UserState;
        }

        /// <summary>
        /// when we're done, re-enable the form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!(bool)e.Result)
            {
                ShowLoadingSpinner();
            }

            SmartToggler();
        }


        #region Control Validation

        private void cboYear_Validating(object sender, CancelEventArgs e)
        {
            //this can be set to a bad value during a job load
            string errMessage = string.Empty;

            bool valid = !string.IsNullOrEmpty(cboYear.SelectedValue as string);
            if (!valid)
            {
                errMessage = "Please select a year from the list";
            }

            this.errorProvider1.SetError(this.cboYear, errMessage);
        }


        private void cboStates_Validating(object sender, CancelEventArgs e)
        {
            //this can now be blank
            string errMessage = string.Empty;

            bool valid = ((AcsState)cboStates.SelectedValue != AcsState.None);
            if (!valid)
            {
                errMessage = "Please select a state from the list";
            }

            this.errorProvider1.SetError(this.cboStates, errMessage);
        }

        private void cboSummaryLevel_Validating(object sender, CancelEventArgs e)
        {
            //this can now be blank
            string errMessage = string.Empty;

            bool valid = ((BoundaryLevels)cboSummaryLevel.SelectedValue != BoundaryLevels.None);
            if (!valid)
            {
                errMessage = "Please select a geographic summary level from the list";
            }

            this.errorProvider1.SetError(this.cboSummaryLevel, errMessage);
        }



        private void txtVariableFilePath_Validating(object sender, CancelEventArgs e)
        {
            //validate variables file
            string errMessage = string.Empty;
            bool valid = FormController.Instance.ValidateVariablesFile(txtVariableFilePath.Text, out errMessage);

            this.errorProvider1.SetError(this.txtVariableFilePath, errMessage);
        }

        private void cboProjections_Validating(object sender, CancelEventArgs e)
        {
            bool listSelected = radioSRIDFromList.Checked;
            bool valueInList = cboProjections.SelectedIndex != -1;
            string errorMessage = string.Empty;


            //if our radio button is selected.
            //if the value is not in the list
            if (listSelected && !valueInList)
            {
                errorMessage = "Unknown SRID";
            }

            this.errorProvider1.SetError(cboProjections, errorMessage);
        }


        private void txtPrjFilePath_Validating(object sender, CancelEventArgs e)
        {
            //if our radio button is selected.
            //if the file doesn't have a valid projection
            bool listSelected = radioSRIDFile.Checked;
            bool validProjectionFile = false;        //TODO
            string errorMessage = string.Empty;

            if (listSelected && !validProjectionFile)
            {
                errorMessage = "Invalid Projection File";
            }

            this.errorProvider1.SetError(txtPrjFilePath, errorMessage);
        }


        private void txtFishnetCellSize_Validating(object sender, CancelEventArgs e)
        {
            //check to see if it parses nicely
            string errorMessage = string.Empty;

            string cellSize = txtFishnetCellSize.Text.Trim();
            if (!string.IsNullOrEmpty(cellSize))
            {
                var chunks = cellSize.Split("x_:, ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                //1 param, square
                if ((chunks.Length == 1)
                    && ((Utilities.GetAs<double>(chunks[0], -1)) == -1))
                {
                    errorMessage = "Invalid size";
                }
                //2 params, rectangle
                else if ((chunks.Length == 2)
                    && (((Utilities.GetAs<double>(chunks[0], -1)) == -1)
                        || ((Utilities.GetAs<double>(chunks[1], -1)) == -1)))
                {
                    errorMessage = "Invalid width/height";
                }
            }

            this.errorProvider1.SetError(txtFishnetCellSize, errorMessage);
        }

        #endregion Control Validation

        #region Dirty State Tracking

        protected bool _isDirty = false;
        public bool IsDirty
        {
            get { return _isDirty; }
            set
            {
                _isDirty = value;
                this.saveJobFileToolStripMenuItem.Text = (_isDirty) ? "*Save Job File" : "Save Job File";
            }
        }

        private void general_SelectedValueChanged(object sender, EventArgs e)
        {
            IsDirty = true;
        }

        private void general_TextChanged(object sender, EventArgs e)
        {
            IsDirty = true;
        }

        private void general_CheckedChanged(object sender, EventArgs e)
        {
            IsDirty = true;
        }

        #endregion Dirty State Tracking

        private void workOfflineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormController.Instance.IsOffline = !FormController.Instance.IsOffline;
            workOfflineToolStripMenuItem.Checked = FormController.Instance.IsOffline;
        }











    }
}