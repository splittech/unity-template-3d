using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Inspector;
using Game.Services.Editor.Common;
using UnityEditor;
using UnityEngine;

namespace Game.Services.Editor.MetaAssetManager
{
    /// <summary>
    /// Lists every meta asset type (menu name under <see cref="GameAssetTypes.MetaAssetMenuPrefix"/>),
    /// shows existing assets and creates new ones in a per-type folder.
    /// </summary>
    internal sealed class MetaAssetManagerWindow : AssetManagerWindow
    {
        [SerializeField]
        [ListViewSettings(ShowAddRemoveFooter = false, Reorderable = true, ShowBoundCollectionSize = false, ShowFoldoutHeader = false)]
        private List<MetaAssetEntry> _entries = new();

        protected override IReadOnlyCollection<string> EntriesPropertyNames { get; } = new[] { nameof(_entries) };

        [MenuItem("Game/Meta Asset Manager")]
        private static void Open()
        {
            Open<MetaAssetManagerWindow>("Meta Asset Manager");
        }

        protected override void CollectEntries()
        {
            MetaAssetFolderSettings settings = MetaAssetFolderSettings.Load();

            _entries = GameAssetTypes.FindCreatableTypes()
                .Where(GameAssetTypes.IsMetaAsset)
                .Select(type => new MetaAssetEntry(type, settings.GetFolder(type)))
                .OrderBy(entry => entry.DisplayName, StringComparer.Ordinal)
                .ToList();
        }

        // Called by Alchemy whenever a field in the window changes, e.g. when a folder is edited.
        protected override void SaveWindowData(string dataPath)
        {
            base.SaveWindowData(dataPath);

            MetaAssetFolderSettings settings = MetaAssetFolderSettings.Load();

            foreach (MetaAssetEntry entry in _entries)
            {
                Type type = entry.ResolveType();

                if (type != null)
                    settings.SetFolder(type, entry.Folder);
            }

            settings.Save();
        }
    }

    [Serializable]
    internal sealed class MetaAssetEntry
    {
        private const string AssetsRoot = "Assets";

        // Unity uses the first string field as the list element label.
        [SerializeField, HideInInspector] private string _displayName;
        [SerializeField, HideInInspector] private string _typeName;
        [SerializeField, HideInInspector] private string _fileName;

        [SerializeField] private string _folder;

        [SerializeField]
        [ListViewSettings(ShowAddRemoveFooter = false, Reorderable = false, ShowBoundCollectionSize = false, ShowFoldoutHeader = false)]
        private List<ScriptableObject> _assets;

        public string DisplayName => _displayName;
        public string Folder => _folder;

        public MetaAssetEntry(Type type, string folder)
        {
            _displayName = GameAssetTypes.GetMenuName(type)[GameAssetTypes.MetaAssetMenuPrefix.Length..];
            _typeName = type.AssemblyQualifiedName;
            _fileName = GameAssetTypes.GetFileName(type);
            _folder = folder;
            _assets = GameAssetTypes.FindAssets(type);
        }

        public Type ResolveType()
        {
            return Type.GetType(_typeName);
        }

        /// <summary>
        /// Creates the asset in the folder configured for this type, creating the folder if needed.
        /// </summary>
        [Button]
        private void Create()
        {
            Type type = ResolveType();

            if (type == null)
            {
                Debug.LogError($"Type '{_typeName}' not found, press Refresh.");
                return;
            }

            // Alchemy may keep a stale copy of this entry after edits, so read the saved folder instead of _folder.
            string folder = MetaAssetFolderSettings.Load().GetFolder(type).Trim().TrimEnd('/');

            if (folder != AssetsRoot && !folder.StartsWith(AssetsRoot + "/", StringComparison.Ordinal))
            {
                Debug.LogError($"Folder '{folder}' for {type.Name} must be inside '{AssetsRoot}/'.");
                return;
            }

            GameAssetTypes.EnsureFolder(folder);
            GameAssetTypes.CreateAsset(_typeName, $"{folder}/{_fileName}.asset");
        }
    }
}
