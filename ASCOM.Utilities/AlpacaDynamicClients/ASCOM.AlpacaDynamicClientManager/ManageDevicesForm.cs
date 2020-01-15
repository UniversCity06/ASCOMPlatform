﻿// This application enables the user to create and manage dynamic client drivers
// The application uses dynamic compilation i.e. the drivers are compiled on the user's machine rather than being delivered through an installer
// Most of the heavy lifting is done through pre-compiled base classes that are called from the dynamically compiled top level shell classes.
// This enables the user to specify what are normally hard coded specifics such as the device type, GUID and device number.

// The application generates required code and stores this in memory. When the class is complete it is compiled and the resultant assembly persisted to disk
// into the same directory as the dynamic client local server, which is then called to register the driver assembly.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ASCOM.Utilities;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace ASCOM.DynamicRemoteClients
{
    public partial class ManageDevicesForm : Form
    {
        // Constants used within this form
        private const string DEVICE_NUMBER = "DeviceNumber"; // Regular expression device number placeholder name
        private const string DEVICE_TYPE = "DeviceType"; // Regular expression device type placeholder name
        private const string PROGID_PARSE_REGEX_STRING = @"^ascom\.alpacadynamic(?'" + DEVICE_NUMBER + @"'\d+)\.(?'" + DEVICE_TYPE + @"'[a-z]+)$"; // Regular expression for extracting device type and number
        private const int NUMBER_OF_DYNAMIC_DRIVER_NUMBERS_TO_TEST = 20; // Number of dynamic drivers whose COM registrations will be tested in order to find an unused dynamic driver progID

        // Global variables within this class
        private TraceLogger TL;
        private Profile profile;
        private List<DynamicDriverRegistration> dynamicDrivers;
        private ColouredCheckedListBox dynamicDriversCheckedListBox;

        #region Initialise and Dispose

        /// <summary>
        /// Initialises the form
        /// </summary>
        public ManageDevicesForm(TraceLogger TLParameter)
        {
            try
            {
                InitializeComponent();

                TL = TLParameter; // Save the supplied trace logger

                Version assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
                LblVersionNumber.Text = "Version " + assemblyVersion.ToString();
                TL.LogMessage("Initialise", string.Format("Application Version: {0}", assemblyVersion.ToString()));

                // Configured the coloured checked list box               
                dynamicDriversCheckedListBox = new ColouredCheckedListBox();
                dynamicDriversCheckedListBox.Parent = this;
                dynamicDriversCheckedListBox.FormattingEnabled = true;
                dynamicDriversCheckedListBox.Location = new Point(41, 65);
                dynamicDriversCheckedListBox.Name = "DynamicDriversCheckedListBox";
                dynamicDriversCheckedListBox.Size = new Size(832, 349);
                dynamicDriversCheckedListBox.TabStop = false;
                dynamicDriversCheckedListBox.HorizontalScrollbar = true;

                profile = new Profile();
                dynamicDrivers = new List<DynamicDriverRegistration>();

                ReadConfiguration(); // Get the current configuration

                // List the drivers found in the log
                foreach (DynamicDriverRegistration driver in dynamicDrivers)
                {
                    TL.LogMessage("Initialise", $"Found remote {driver.DeviceType} driver: {driver.Description}");
                }

                TL.LogMessage("Initialise", string.Format("Initialisation completed"));
            }
            catch (Exception ex)
            {
                TL.LogMessageCrLf("initialise - Exception", ex.ToString());
                MessageBox.Show("Sorry, en error occurred on start up, please report this error message on the ASCOM Talk forum hosted at Groups.Io.\r\n\n" + ex.Message, "ASCOM Dynamic Clients", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            if (profile != null) try { profile.Dispose(); } catch { }
            if (dynamicDriversCheckedListBox != null) try { dynamicDriversCheckedListBox.Dispose(); } catch { }

            base.Dispose(disposing);
        }

        #endregion

        /// <summary>
        /// Creates a list of Alpaca dynamic drivers and their configuration information
        /// </summary>
        private void ReadConfiguration()
        {
            Regex progidParseRegex = new Regex(PROGID_PARSE_REGEX_STRING, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            string driverDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86) + SharedConstants.ALPACA_CLIENT_LOCAL_SERVER_PATH;

            // Initialise 
            dynamicDrivers.Clear();
            dynamicDriversCheckedListBox.Items.Clear();

            // Extract a list of the remote client drivers from the list of devices in the Profile
            ArrayList deviceTypes = profile.RegisteredDeviceTypes;
            foreach (string deviceType in deviceTypes)
            {
                ArrayList devices = profile.RegisteredDevices(deviceType);
                foreach (KeyValuePair device in devices)
                {
                    Match match = progidParseRegex.Match(device.Key); // Parse the ProgID to extract the device number and device type
                    if (match.Success)
                    {
                        // Create a new data class to hold information about this dynamic driver
                        DynamicDriverRegistration foundDriver = new DynamicDriverRegistration();

                        try
                        {
                            // Populate information from the dynamic driver's ProgID
                            foundDriver.ProgId = match.Groups["0"].Value;
                            foundDriver.Number = int.Parse(match.Groups[DEVICE_NUMBER].Value, CultureInfo.InvariantCulture);
                            foundDriver.DeviceType = deviceType;

                            // populate configuration information from the dynamic driver's Profile and 
                            profile.DeviceType = foundDriver.DeviceType;
                            foundDriver.IPAdrress = profile.GetValue(foundDriver.ProgId, SharedConstants.IPADDRESS_PROFILENAME);
                            foundDriver.PortNumber = Convert.ToInt32(profile.GetValue(foundDriver.ProgId, SharedConstants.PORTNUMBER_PROFILENAME));
                            foundDriver.RemoteDeviceNumber = Convert.ToInt32(profile.GetValue(foundDriver.ProgId, SharedConstants.REMOTE_DEVICE_NUMBER_PROFILENAME));
                            foundDriver.UniqueID = profile.GetValue(foundDriver.ProgId, SharedConstants.UNIQUEID_PROFILENAME);
                            foundDriver.Name = device.Value;
                            foundDriver.Description = $"{foundDriver.Name} ({foundDriver.ProgId}) - {foundDriver.IPAdrress}:{foundDriver.PortNumber}/api/v1/{foundDriver.DeviceType}/{foundDriver.RemoteDeviceNumber} - {foundDriver.UniqueID}";

                            // Test whether the driver is correctly registered and installed and flag accordingly
                            string compatibilityMessage = VersionCode.DriverCompatibilityMessage(foundDriver.ProgId, VersionCode.Bitness.Bits32, TL);

                            if (compatibilityMessage == "") // Driver is correctly registered 
                            {
                                string driverExecutable = $"{driverDirectory}{foundDriver.ProgId}.dll";
                                TL.LogMessage("ReadConfiguration", $"Searching for driver:  {driverExecutable}");

                                // Confirm that the driver DLL executable exists
                                if (File.Exists(driverExecutable)) // DLL exists so flag OK
                                {
                                    foundDriver.InstallState = InstallationState.Ok;
                                }
                                else // Driver DLL does not exist so flag as corrupted and suggest deletion
                                {
                                    foundDriver.InstallState = InstallationState.MissingDriver;
                                    foundDriver.Description = $"{foundDriver.ProgId} - Driver is ASCOM registered does not exist - Deletion recommended";
                                }
                            }
                            else // A driver installation issue was found so flag as corrupted and recommend deletion
                            {
                                foundDriver.InstallState = InstallationState.NotCompatible;
                                foundDriver.Description = $"{foundDriver.ProgId} - {compatibilityMessage} - Deletion recommended";
                            }

                            TL.LogMessage("ReadConfiguration", $"{foundDriver.ProgId} - {compatibilityMessage} - Installed OK: {foundDriver.InstallState}");
                        }
                        catch (Exception ex)
                        {
                            foundDriver.InstallState = InstallationState.BadProfile;
                            foundDriver.Description = $"{foundDriver.ProgId} - ASCOM Profile is invalid - Deletion recommended";
                            TL.LogMessageCrLf("ReadConfiguration", ex.ToString());
                        }
                        finally
                        {
                            // Add the data class to the dynamic devices collection and to the form's checked list box
                            dynamicDrivers.Add(foundDriver);
                            dynamicDriversCheckedListBox.Items.Add(foundDriver);

                            TL.LogMessage("ReadConfiguration", $"{foundDriver.ProgId} - {foundDriver.Number} - {foundDriver.DeviceType} - Installed OK: {foundDriver.InstallState}");
                        }
                    }
                }

                // Test the first N COM registrations of the form ASCOM.AlpacaDynamic{X}.{DeviceType} and check whether they are registered for COM and whether their DLL executables exist. If not flag as corrupt
                for (int i = 1; i <= NUMBER_OF_DYNAMIC_DRIVER_NUMBERS_TO_TEST; i++)
                {
                    string progId = $"{SharedConstants.DRIVER_PROGID_BASE}{i}.{deviceType}";
                    Type typeFromProgId = Type.GetTypeFromProgID(progId);

                    if (typeFromProgId != null) // This ProgID is registered
                    {
                        TL.LogMessage("ReadConfiguration", $"ProgID {progId} is registered. Type name: {typeFromProgId?.Name}");

                        string driverExecutable = $"{driverDirectory}{progId}.dll";
                        TL.LogMessage("ReadConfiguration", $"Searching for driver:  {driverExecutable}");

                        // Test whether the driver DLL executable is missing
                        if (!File.Exists(driverExecutable))  // Driver DLL does not exist so flag as corrupted and suggest deletion
                        {
                            // Only add this driver to the list if it is not already in the list
                            if (dynamicDrivers.Where(x => x.ProgId.ToLowerInvariant() == progId.ToLowerInvariant()).Count() == 0) // There are no drivers with this ProgID already in the list
                            {
                                // Create a new list entry
                                DynamicDriverRegistration foundDriver = new DynamicDriverRegistration();
                                foundDriver.ProgId = progId;
                                foundDriver.DeviceType = deviceType;
                                foundDriver.Name = progId;
                                foundDriver.InstallState = InstallationState.MissingDriver;
                                foundDriver.Description = $"{foundDriver.ProgId} - Driver is COM registered but driver executable does not exist - Deletion recommended";

                                dynamicDrivers.Add(foundDriver);
                                dynamicDriversCheckedListBox.Items.Add(foundDriver);
                                TL.LogMessage("ReadConfiguration", $"Adding driver to deletion list:  {progId}");
                            }
                            else // A driver with this ProgID is already in the list so no need to add it again, which would just create a duplicate entry
                            {
                                TL.LogMessage("ReadConfiguration", $"{progId} - A driver with this ProgID already exists - no action taken");
                                // No action
                            }
                        }
                        else // Driver is COM registered and its driver DLL exists
                        {
                            // Test whether the device is ASCOM registered
                            if (!profile.IsRegistered(progId)) // The driver is not registered in the ASCOM Profile
                            {
                                // Create a new list entry
                                DynamicDriverRegistration foundDriver = new DynamicDriverRegistration();
                                foundDriver.ProgId = progId;
                                foundDriver.DeviceType = deviceType;
                                foundDriver.Name = progId;
                                foundDriver.InstallState = InstallationState.BadProfile;
                                foundDriver.Description = $"{foundDriver.ProgId} - Driver is COM registered but not ASCOM registered - Deletion recommended";

                                dynamicDrivers.Add(foundDriver);
                                dynamicDriversCheckedListBox.Items.Add(foundDriver);
                                TL.LogMessage("ReadConfiguration", $"Adding driver to deletion list:  {progId}");
                            }
                            else // The driver is registered in the ASCOM Profile
                            {
                                // No action required
                                TL.LogMessage("ReadConfiguration", $"{progId} - Driver DLL exists and is registered for COM and ASCOM so this driver will have been already listed - no action taken");
                            }
                        }
                    }
                    else // This progID is not registered
                    {
                        // No action required
                        TL.LogMessage("ReadConfiguration", $"{progId} - ProgID is not COM registered - no action taken");
                    }
                }
            }
        }

        /// <summary>
        /// Cancel button handler - just closes the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                TL.LogMessage("Cancel", "Closing the application");
            }
            finally
            {
                Application.Exit();
            }
        }

        /// <summary>
        /// Completely remove drivers flagged for deletion
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDeleteDrivers_Click(object sender, EventArgs e)
        {
            // Confirm whether the user really does want to delete the selected drivers
            DialogResult result = MessageBox.Show("Are you sure that you want to delete the checked drivers?", "Delete Dynamic Drivers", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return; // Give up if there is any outcome other than yes

            try
            {
                // Disable controls so that the process can't be stopped part way through 
                BtnDeleteDrivers.Enabled = false;

                // Create pointer to the dynamic driver's local server folder
                string localServerPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86) + SharedConstants.ALPACA_CLIENT_LOCAL_SERVER_PATH;
                TL.LogMessage("DeleteDrivers", $"Local server path: {localServerPath}");

                // Iterate over each device that has been checked in the UI checked list box
                foreach (DynamicDriverRegistration driver in dynamicDriversCheckedListBox.CheckedItems)
                {
                    // COM unregister the driver
                    ComUnRegister(driver.ProgId);

                    // Create pointers to the driver executable and its PDB file
                    TL.LogMessage("DeleteDrivers", $"Deleting driver {driver.Description}");
                    string driverFileName = $"{localServerPath}{driver.ProgId}.dll";
                    string pdbFileName = $"{localServerPath}{driver.ProgId}.pdb";

                    // Delete the driver and it's PDB file
                    TL.LogMessage("DeleteDrivers", $"Deleting driver files {driverFileName} and {pdbFileName}");
                    try
                    {
                        File.Delete(driverFileName);
                        TL.LogMessage("DeleteDrivers", $"Successfully deleted driver file { driverFileName}");
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"Unable to delete driver file {driverFileName} - {ex.Message}";
                        TL.LogMessageCrLf("DeleteDrivers", $"{errorMessage} \r\n{ex.ToString()}");
                        MessageBox.Show(errorMessage, "Alpaca Dynamic Client Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    try
                    {
                        File.Delete(pdbFileName);
                        TL.LogMessage("DeleteDrivers", $"Successfully deleted driver file { driverFileName}");
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"Unable to delete driver file {pdbFileName} - {ex.Message}";
                        TL.LogMessageCrLf("DeleteDrivers", $"{errorMessage} \r\n{ex.ToString()}");
                        MessageBox.Show(errorMessage, "Alpaca Dynamic Client Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    // Remove the ASCOM Profile information that is not removed by the local server when the driver is unregistered
                    try
                    {
                        TL.LogMessage("DeleteDrivers", $"Removing driver Profile registration for {driver.DeviceType} driver: {driver.ProgId}");
                        profile.DeviceType = driver.DeviceType;
                        profile.Unregister(driver.ProgId);
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"Unable to unregister driver {driver.ProgId} - {ex.Message}";
                        TL.LogMessageCrLf("DeleteDrivers", $"{errorMessage} \r\n{ex.ToString()}");
                        MessageBox.Show(errorMessage, "Alpaca Dynamic Client Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    // Remove the flag indicating that this driver has been configured
                    try
                    {
                        using (RegistryAccess registryAccess = new RegistryAccess())
                        {
                            registryAccess.DeleteProfile("Chooser", $"{driver.ProgId} Init");
                        }
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"Unable to remove driver initialised flag for {driver.ProgId} - {ex.Message}";
                        TL.LogMessageCrLf("DeleteDrivers", $"{errorMessage} \r\n{ex.ToString()}");
                        MessageBox.Show(errorMessage, "Alpaca Dynamic Client Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                ReadConfiguration();
            }
            catch (Exception ex)
            {
                TL.LogMessageCrLf("DeleteDrivers - Exception", ex.ToString());
                MessageBox.Show("Sorry, en error occurred during Apply, please report this error message on the ASCOM Talk forum hosted at Groups.Io.\r\n\n" + ex.Message, "ASCOM Dynamic Clients", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                BtnDeleteDrivers.Enabled = true;
            }
        }

        /// <summary>
        /// Unregister a COM driver
        /// </summary>
        /// <param name="progId">ProgID of the driver to be unregistered</param>
        private void ComUnRegister(string progId)
        {
            try
            {
                RegistryKey classIdKey = Registry.ClassesRoot.OpenSubKey($"{progId}\\CLSID");

                if (classIdKey != null)
                {
                    try
                    {
                        string classId = (string)classIdKey.GetValue("");
                        if (!string.IsNullOrEmpty(classId)) // We have a class ID value
                        {
                            TL.LogMessage("ComUnregister", $"Deleting ProgID {progId}, which has a class ID of: {classId}");

                            // Delete the ProgID entries in the 32bit registry section
                            Registry.ClassesRoot.DeleteSubKey($"{progId}\\CLSID", false);
                            Registry.ClassesRoot.DeleteSubKey(progId, false);

                            // Delete the CLSID entries in the 32bit registry section
                            Registry.ClassesRoot.DeleteSubKey($"CLSID\\{classId}\\Implemented Categories\\{{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}}", false);
                            Registry.ClassesRoot.DeleteSubKey($"CLSID\\{classId}\\Implemented Categories", false);
                            Registry.ClassesRoot.DeleteSubKey($"CLSID\\{classId}\\ProgId", false);
                            Registry.ClassesRoot.DeleteSubKey($"CLSID\\{classId}\\LocalServer32", false);
                            Registry.ClassesRoot.DeleteSubKey($"CLSID\\{classId}\\Programmable", false);
                            Registry.ClassesRoot.DeleteSubKey($"CLSID\\{classId}", false);

                            TL.LogMessage("ComUnregister", $"Deleted ProgID {progId}");
                        }
                        else
                        {
                            // Cannot get the class ID value
                            TL.LogMessage("ComUnregister", $"Cannot find the class ID value - cannot proceed further");
                        }
                    }
                    catch (Exception ex)
                    {
                        TL.LogMessageCrLf("ComUnregister", $"Exception retrieving CLSID, cannot proceed further: \r\n{ex.ToString()}");
                    }
                }
                else
                {
                    // Cannot open the CLSID key to read the class ID
                    TL.LogMessage("ComUnregister", $"Cannot open the CLSID key - cannot proceed further");
                }
            }
            catch (Exception ex)
            {
                TL.LogMessageCrLf("ComUnregister", $"Exception opening CLSID key, cannot proceed further: \r\n{ex.ToString()}");
            }
        }

        /// <summary>
        /// Check or uncheck all driver entries when the "Select All" checkbox state is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            for (int i = 0; i < dynamicDriversCheckedListBox.Items.Count; i++)
            {
                dynamicDriversCheckedListBox.SetItemChecked(i, checkBox.Checked);
            }
        }
    }
}