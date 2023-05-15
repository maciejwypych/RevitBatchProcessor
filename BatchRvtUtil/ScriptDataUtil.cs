//
// Revit Batch Processor
//
// Copyright (c) 2020  Daniel Rumery, BVN
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
//

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace BatchRvtUtil;

public static class ScriptDataUtil
{
    private const string SCRIPT_DATA_FILENAME_PREFIX = "Session.ScriptData.";
    private const string SESSION_PROGRESS_RECORD_PREFIX = "Session.ProgressRecord.";
    private const string JSON_FILE_EXTENSION = ".json";

    public static IEnumerable<ScriptData> LoadManyFromFile(string filePath)
    {
        List<ScriptData> scriptDatas = null;

        if (!File.Exists(filePath)) return null;
        try
        {
            var text = File.ReadAllText(filePath);

            var jarray = JsonUtil.DeserializeArrayFromJson(text);

            scriptDatas = new List<ScriptData>();

            foreach (var jtoken in jarray)
            {
                var jobject = jtoken as JObject;

                if (jobject == null) continue;
                var scriptData = new ScriptData();

                scriptData.Load(jobject);

                scriptDatas.Add(scriptData);
            }

            return scriptDatas;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public static bool SaveManyToFile(string filePath, IEnumerable<ScriptData> scriptDatas)
    {
        var success = false;

        try
        {
            var jarray = new JArray();

            foreach (var scriptData in scriptDatas)
            {
                var jobject = new JObject();

                scriptData.Store(jobject);

                jarray.Add(jobject);
            }

            var settingsText = JsonUtil.SerializeToJson(jarray, true);

            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Directory != null) fileInfo.Directory.Create();

            File.WriteAllText(fileInfo.FullName, settingsText);

            success = true;
        }
        catch (Exception e)
        {
            success = false;
        }

        return success;
    }

    public static string GetUniqueScriptDataFilePath()
    {
        var uniqueId = Guid.NewGuid().ToString();

        return Path.Combine(
            BatchRvt.GetDataFolderPath(),
            SCRIPT_DATA_FILENAME_PREFIX + uniqueId + JSON_FILE_EXTENSION
        );
    }

    public static string GetProgressRecordFilePath(string scriptDataFilePath)
    {
        var uniqueId = Path.GetFileNameWithoutExtension(scriptDataFilePath)
            .Substring(SCRIPT_DATA_FILENAME_PREFIX.Length);

        return Path.Combine(
            Path.GetDirectoryName(scriptDataFilePath),
            SESSION_PROGRESS_RECORD_PREFIX + uniqueId + JSON_FILE_EXTENSION
        );
    }

    public static bool SetProgressNumber(string progressRecordFilePath, int progressNumber)
    {
        try
        {
            var fileInfo = new FileInfo(progressRecordFilePath);

            if (fileInfo.Directory != null)
                fileInfo.Directory.Create();

            File.WriteAllText(fileInfo.FullName, progressNumber.ToString());

            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public static int? GetProgressNumber(string progressRecordFilePath)
    {
        int? progressNumber = null;

        try
        {
            var fileInfo = new FileInfo(progressRecordFilePath);

            progressNumber = int.Parse(File.ReadAllText(fileInfo.FullName).Trim());
        }
        catch (Exception e)
        {
            progressNumber = null;
        }

        return progressNumber;
    }

    public class ScriptData : IPersistent
    {
        public readonly ListSetting<string> AssociatedData = new("associatedData");
        public readonly BooleanSetting AuditOnOpening = new("auditOnOpening");

        public readonly EnumSetting<BatchRvt.CentralFileOpenOption>
            CentralFileOpenOption = new("centralFileOpenOption");

        public readonly StringSetting CloudModelId = new("cloudModelId");
        public readonly StringSetting CloudProjectId = new("cloudProjectId");
        public readonly StringSetting DataExportFolderPath = new("dataExportFolderPath");
        public readonly BooleanSetting DeleteLocalAfter = new("deleteLocalAfter");
        public readonly BooleanSetting DiscardWorksetsOnDetach = new("discardWorksetsOnDetach");
        public readonly BooleanSetting EnableDataExport = new("enableDataExport");
        public readonly BooleanSetting IsCloudModel = new("isCloudModel");
        public readonly BooleanSetting OpenInUI = new("openInUI");
        private readonly PersistentSettings persistentSettings;
        public readonly IntegerSetting ProgressMax = new("progressMax");
        public readonly IntegerSetting ProgressNumber = new("progressNumber");
        public readonly StringSetting RevitFilePath = new("revitFilePath");

        public readonly EnumSetting<BatchRvt.RevitProcessingOption>
            RevitProcessingOption = new("revitProcessingOption");

        public readonly StringSetting SessionDataFolderPath = new("sessionDataFolderPath");

        public readonly StringSetting SessionId = new("sessionId");
        public readonly BooleanSetting ShowMessageBoxOnTaskScriptError = new("showMessageBoxOnTaskError");
        public readonly StringSetting TaskData = new("taskData");
        public readonly StringSetting TaskScriptFilePath = new("taskScriptFilePath");

        public readonly EnumSetting<BatchRvt.WorksetConfigurationOption> WorksetConfigurationOption =
            new("worksetConfigurationOption");

        public ScriptData()
        {
            persistentSettings = new PersistentSettings(
                new IPersistent[]
                {
                    SessionId,
                    RevitFilePath,
                    IsCloudModel,
                    CloudProjectId,
                    CloudModelId,
                    EnableDataExport,
                    TaskScriptFilePath,
                    TaskData,
                    SessionDataFolderPath,
                    DataExportFolderPath,
                    ShowMessageBoxOnTaskScriptError,
                    RevitProcessingOption,
                    CentralFileOpenOption,
                    DeleteLocalAfter,
                    DiscardWorksetsOnDetach,
                    WorksetConfigurationOption,
                    OpenInUI,
                    AuditOnOpening,
                    ProgressNumber,
                    ProgressMax,
                    AssociatedData
                }
            );
        }

        public void Load(JObject jobject)
        {
            persistentSettings.Load(jobject);
        }

        public void Store(JObject jobject)
        {
            persistentSettings.Store(jobject);
        }

        public bool LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return false;
            try
            {
                var text = File.ReadAllText(filePath);
                var jobject = JsonUtil.DeserializeFromJson(text);
                persistentSettings.Load(jobject);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        public bool SaveToFile(string filePath)
        {
            var success = false;

            var jobject = new JObject();

            try
            {
                persistentSettings.Store(jobject);
                var settingsText = JsonUtil.SerializeToJson(jobject, true);
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Directory != null) fileInfo.Directory.Create();
                File.WriteAllText(fileInfo.FullName, settingsText);

                success = true;
            }
            catch (Exception e)
            {
                success = false;
            }

            return success;
        }

        public string ToJsonString()
        {
            var jobject = new JObject();
            Store(jobject);
            return jobject.ToString();
        }

        public static ScriptData FromJsonString(string scriptDataJson)
        {
            ScriptData scriptData = null;

            try
            {
                var jobject = JsonUtil.DeserializeFromJson(scriptDataJson);
                scriptData = new ScriptData();
                scriptData.Load(jobject);
            }
            catch (Exception e)
            {
                scriptData = null;
            }

            return scriptData;
        }
    }
}