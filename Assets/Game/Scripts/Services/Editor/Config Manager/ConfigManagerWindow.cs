using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Inspector;
using Game.Services.Editor.Common;
using UnityEditor;
using UnityEngine;

namespace Game.Services.Editor.ConfigManager
{
    /// <summary>
    /// Lists every creatable ScriptableObject from Game.* assemblies except meta assets,
    /// shows existing assets of each type and creates new ones.
    /// Configs from the Game.Services assembly go to the Services tab, all others to the Game tab.
    /// </summary>
    internal sealed class ConfigManagerWindow : AssetManagerWindow
    {
        private const string TabGroupName = "Configs";
        private const string ServicesAssemblyName = "Game.Services";

        [SerializeField]
        [TabGroup(TabGroupName, "Game")]
        [ListViewSettings(ShowAddRemoveFooter = false, Reorderable = true, ShowBoundCollectionSize = false, ShowFoldoutHeader = false)]
        private List<ConfigEntry> _gameConfigs = new();

        [SerializeField]
        [TabGroup(TabGroupName, "Services")]
        [ListViewSettings(ShowAddRemoveFooter = false, Reorderable = true, ShowBoundCollectionSize = false, ShowFoldoutHeader = false)]
        private List<ConfigEntry> _serviceConfigs = new();

        protected override IReadOnlyCollection<string> EntriesPropertyNames { get; } =
            new[] { nameof(_gameConfigs), nameof(_serviceConfigs) };

        [MenuItem("Game/Config Manager")]
        private static void Open()
        {
            Open<ConfigManagerWindow>("Config Manager");
        }

        protected override void CollectEntries()
        {
            List<Type> types = GameAssetTypes.FindCreatableTypes()
                .Where(type => !GameAssetTypes.IsMetaAsset(type))
                .ToList();

            _gameConfigs = CreateEntries(types.Where(type => !IsServiceConfig(type)));
            _serviceConfigs = CreateEntries(types.Where(IsServiceConfig));
        }

        private static bool IsServiceConfig(Type type)
        {
            return type.Assembly.GetName().Name == ServicesAssemblyName;
        }

        private static List<ConfigEntry> CreateEntries(IEnumerable<Type> types)
        {
            return types
                .Select(type => new ConfigEntry(type))
                .OrderBy(entry => entry.DisplayName, StringComparer.Ordinal)
                .ToList();
        }
    }

    [Serializable]
    internal sealed class ConfigEntry
    {
        // Unity uses the first string field as the list element label.
        [SerializeField, HideInInspector] private string _displayName;
        [SerializeField, HideInInspector] private string _typeName;
        [SerializeField, HideInInspector] private string _fileName;

        [SerializeField]
        [ListViewSettings(ShowAddRemoveFooter = false, Reorderable = false, ShowBoundCollectionSize = false, ShowFoldoutHeader = false)]
        private List<ScriptableObject> _assets;

        public string DisplayName => _displayName;

        public ConfigEntry(Type type)
        {
            string menuName = GameAssetTypes.GetMenuName(type);

            _displayName = menuName.StartsWith(GameAssetTypes.ConfigMenuPrefix, StringComparison.Ordinal)
                ? menuName[GameAssetTypes.ConfigMenuPrefix.Length..]
                : menuName;
            _typeName = type.AssemblyQualifiedName;
            _fileName = GameAssetTypes.GetFileName(type);
            _assets = GameAssetTypes.FindAssets(type);
        }

        /// <summary>
        /// Creates the asset in the folder selected in the Project window.
        /// </summary>
        [Button]
        private void Create()
        {
            GameAssetTypes.CreateAsset(_typeName, $"{_fileName}.asset");
        }
    }
}
