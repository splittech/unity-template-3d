using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Game.Services.Editor.Common
{
    /// <summary>
    /// Discovers ScriptableObject types with <see cref="CreateAssetMenuAttribute"/> from Game.* assemblies.
    /// </summary>
    internal static class GameAssetTypes
    {
        public const string ConfigMenuPrefix = "Game/Configs/";
        public const string MetaAssetMenuPrefix = "Game/Meta Assets/";

        private const string GameAssemblyPrefix = "Game.";

        public static IEnumerable<Type> FindCreatableTypes()
        {
            return TypeCache.GetTypesWithAttribute<CreateAssetMenuAttribute>()
                .Where(type => type.Assembly.GetName().Name.StartsWith(GameAssemblyPrefix, StringComparison.Ordinal))
                .Where(type => !type.IsAbstract && typeof(ScriptableObject).IsAssignableFrom(type));
        }

        public static bool IsMetaAsset(Type type)
        {
            return GetMenuName(type).StartsWith(MetaAssetMenuPrefix, StringComparison.Ordinal);
        }

        public static string GetMenuName(Type type)
        {
            string menuName = type.GetCustomAttribute<CreateAssetMenuAttribute>().menuName;
            return string.IsNullOrEmpty(menuName) ? type.Name : menuName;
        }

        public static string GetFileName(Type type)
        {
            string fileName = type.GetCustomAttribute<CreateAssetMenuAttribute>().fileName;
            return string.IsNullOrEmpty(fileName) ? $"New {type.Name}" : fileName;
        }

        public static List<ScriptableObject> FindAssets(Type type)
        {
            // The t: filter matches by short type name, so filter out same-named types from other namespaces.
            return AssetDatabase.FindAssets($"t:{type.Name}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null && asset.GetType() == type)
                .ToList();
        }

        /// <summary>
        /// Creates the asset and starts inline renaming in the Project window.
        /// A relative <paramref name="pathName"/> is resolved against the folder selected in the Project window.
        /// </summary>
        public static void CreateAsset(string assemblyQualifiedTypeName, string pathName)
        {
            Type type = Type.GetType(assemblyQualifiedTypeName);

            if (type == null)
            {
                Debug.LogError($"Type '{assemblyQualifiedTypeName}' not found, press Refresh.");
                return;
            }

            ProjectWindowUtil.CreateAsset(ScriptableObject.CreateInstance(type), pathName);
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            int separatorIndex = folder.LastIndexOf('/');
            string parent = folder[..separatorIndex];
            string name = folder[(separatorIndex + 1)..];

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
