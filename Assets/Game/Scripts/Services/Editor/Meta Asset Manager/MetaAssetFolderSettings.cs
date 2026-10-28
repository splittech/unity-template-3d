using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Services.Editor.MetaAssetManager
{
    /// <summary>
    /// Per-type folders for meta assets, stored in ProjectSettings so they are shared via version control.
    /// Only folders that differ from the default are stored.
    /// </summary>
    [Serializable]
    internal sealed class MetaAssetFolderSettings
    {
        public const string FilePath = "ProjectSettings/Game.MetaAssetFolders.json";

        private const string DefaultRootFolder = "Assets/Game/Meta Assets";

        [SerializeField] private List<MetaAssetFolder> _folders = new();

        public static string GetDefaultFolder(Type type)
        {
            return $"{DefaultRootFolder}/{ObjectNames.NicifyVariableName(type.Name)}";
        }

        public static MetaAssetFolderSettings Load()
        {
            MetaAssetFolderSettings settings = new();

            if (File.Exists(FilePath))
                JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath), settings);

            return settings;
        }

        public void Save()
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(this, true));
        }

        public string GetFolder(Type type)
        {
            MetaAssetFolder entry = _folders.Find(folder => folder.TypeName == type.FullName);
            return entry != null ? entry.Folder : GetDefaultFolder(type);
        }

        public void SetFolder(Type type, string folder)
        {
            _folders.RemoveAll(entry => entry.TypeName == type.FullName);

            if (!string.IsNullOrWhiteSpace(folder) && folder != GetDefaultFolder(type))
                _folders.Add(new MetaAssetFolder { TypeName = type.FullName, Folder = folder });
        }

        [Serializable]
        private sealed class MetaAssetFolder
        {
            public string TypeName;
            public string Folder;
        }
    }
}
