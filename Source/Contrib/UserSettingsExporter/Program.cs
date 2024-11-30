// COPYRIGHT 2024 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// Export the User Settings in the Registry into an INI file.
// In order to support multiple installations with different settings,
// OpenRails supports using an INI file for the settings (instead of
// the registry entries that are shared by all installations).
// OpenRails creates a default INI file when the file exists, but does
// not have valid settings.
//
// The UserSettingsExporter scans the registry and copies all settings
// finds into the INI file. If an INI file already exists, it is backed
// up.
// An already existing backup file is temporarily saved as a to-delete
// file. This preserves the backed up settings after a failure to create
// the INI file.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using ORTS.Common;

namespace ORTS.UserSettingsExporter
{
    class Program
    {
        static int NumExported = 0;

        static void Main(string[] args)
        {
            bool optionVerbose = OptionsContain(args, new string[] { "/v", "/verbose" });
            bool optionHelp = OptionsContain(args, new string[] { "/h", "/help" });
            if (optionHelp)
            {
                ShowHelp();
                return;
            }

            // from UserSettings.cs
            const string defaultRegistryKeyName = "SOFTWARE\\OpenRails\\ORTS";
            const string defaultSection = "ORTS";
            string settingsFilePath = Path.Combine(ApplicationInfo.ProcessDirectory, "OpenRails.ini");

            string backupFilePath = settingsFilePath + ".bakup";
            string deleteFilePath = settingsFilePath + ".delete";

            if (File.Exists(deleteFilePath))
            {
                Console.WriteLine("Warning: The preceding run seems to have failed.");
                Console.WriteLine("Warning: There is a stale backup file, {0}.", backupFilePath);
                Console.WriteLine("Warning: Please manually restore from it, or delete it.");
                Environment.Exit(1);
            }

            if (File.Exists(settingsFilePath)) 
            {
                if (File.Exists(backupFilePath)) { File.Move(backupFilePath, deleteFilePath); }
                File.Copy(settingsFilePath, backupFilePath);
                Console.WriteLine("Info: Backed up existing INI file as {0}.", backupFilePath);
            }
            else 
            { 
                Console.WriteLine("Info: Creating a new INI file as {0}.", settingsFilePath);
                File.Create(settingsFilePath); // SettingsStore requires that the file exists.
            }

            // default section (ORTS)
            RegistryKey defaultRegKey = Registry.CurrentUser.CreateSubKey(defaultRegistryKeyName);
            ExportSection(defaultRegKey, settingsFilePath, defaultSection);

            // subsections
            string[] subKeyNames = defaultRegKey.GetSubKeyNames();
            foreach (var section in subKeyNames)
            {
                RegistryKey sectionRegKey = Registry.CurrentUser.CreateSubKey(defaultRegistryKeyName + @"\" + section);
                ExportSection(sectionRegKey, settingsFilePath, section);
            }

            // cleanup on success
            if (File.Exists(deleteFilePath)) { File.Delete(deleteFilePath); }

            Console.WriteLine("Info: Exported {0} settings from registry {1} into INI file {2}.",
                NumExported, defaultRegKey.Name, settingsFilePath);
            Console.WriteLine("Info: Please review the INI file, using Notepad or another text editor.");
        }

        static void ExportSection(RegistryKey sectionKey, string settingsFilePath, string section)
        {
            SettingsStore iniSettingsStore = SettingsStore.GetSettingStore(settingsFilePath, null, section);

            string[] regKeyNames = sectionKey.GetValueNames();
            foreach (var name in regKeyNames)
            {
                object value = sectionKey.GetValue(name);
                if (value != null)
                {
                    switch (value)
                    {
                        case bool b: iniSettingsStore.SetUserValue(name, b); break;
                        case int i: iniSettingsStore.SetUserValue(name, i); break;
                        case long l: iniSettingsStore.SetUserValue(name, l); break;
                        case DateTime dt: iniSettingsStore.SetUserValue(name, dt); break;
                        case TimeSpan ts: iniSettingsStore.SetUserValue(name, ts); break;
                        case string s: iniSettingsStore.SetUserValue(name, s); break;
                        case int[] ia: iniSettingsStore.SetUserValue(name, ia); break;
                        case string[] sa: iniSettingsStore.SetUserValue(name, sa); break;
                        default:
                            Console.WriteLine("Error: Registry key type {0} is not supported, skipping key {1}.{2}.", value.GetType(), section, name);
                            break;
                    }
                    NumExported++;
                }
            }
        }

            static bool OptionsContain(string[] args, IEnumerable<string> optionNames)
        {
            return optionNames.Any((option) => args.Contains(option, StringComparer.OrdinalIgnoreCase));
        }

        static void ShowHelp()
        {
            Console.WriteLine("{0} {1}", ApplicationInfo.ApplicationName, VersionInfo.VersionOrBuild);
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  {0} [options]", Path.GetFileNameWithoutExtension(ApplicationInfo.ProcessFile));
            Console.WriteLine();
            Console.WriteLine("Arguments: none");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  /h, /help        Show help.");
            Console.WriteLine("  /v, /verbose     Displays all expected/valid values in addition to any errors");
            Console.WriteLine();
            Console.WriteLine("This utility reads the UserSettings (Options) from the registry, and ");
            Console.WriteLine("creates an OpenRails.ini file with the same values. The file is create ");
            Console.WriteLine("in the same directory as this program is run from.");
            Console.WriteLine();
            Console.WriteLine("When an OpenRails.ini file already exists, it is first saved as OpenRails.ini.save");
        }


    }

    // copied from SettingsStore.cs
    public static class NativeMethods
    {
        /// <summary>
        /// Copies a string into the specified section of an initialization file.
        /// </summary>
        /// <param name="sectionName">The name of the section to which the string will be copied. If the section does not exist, it is created. The name of the section is case-independent; the string can be any combination of uppercase and lowercase letters.</param>
        /// <param name="keyName">The name of the key to be associated with a string. If the key does not exist in the specified section, it is created. If this parameter is <c>null</c>, the entire section, including all entries within the section, is deleted.</param>
        /// <param name="value">A <c>null</c>-terminated string to be written to the file. If this parameter is <c>null</c>, the key pointed to by the lpKeyName parameter is deleted. </param>
        /// <param name="fileName">The name of the initialization file.
        /// If the file was created using Unicode characters, the function writes Unicode characters to the file. Otherwise, the function writes ANSI characters.</param>
        /// <returns>If the function successfully copies the string to the initialization file, the return value is nonzero.
        /// If the function fails, or if it flushes the cached version of the most recently accessed initialization file, the return value is zero. To get extended error information, call GetLastError.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int WritePrivateProfileString(string sectionName, string keyName, string value, string fileName);
    }
}
