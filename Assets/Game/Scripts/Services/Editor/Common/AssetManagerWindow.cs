using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alchemy.Editor;
using Alchemy.Inspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Services.Editor.Common
{
    /// <summary>
    /// Base window that rebuilds its entry list from the project on open and on every project change.
    /// </summary>
    internal abstract class AssetManagerWindow : AlchemyEditorWindow
    {
        private const float DefaultWidth = 500f;
        private const float DefaultHeight = 600f;
        private const float EntrySpacing = 10f;

        // Every entry must have this serialized string field; it identifies the entry in the saved order.
        private const string EntryKeyPropertyName = "_typeName";

        private ScrollView _scrollView;

        // Entry order is a per-user preference, so it goes to UserSettings, which is not under version control.
        private string EntryOrderPath => $"UserSettings/{GetType().FullName}.EntryOrder.json";

        /// <summary>
        /// Names of the serialized list fields holding the entries.
        /// </summary>
        protected abstract IReadOnlyCollection<string> EntriesPropertyNames { get; }

        protected abstract void CollectEntries();

        /// <summary>
        /// Opens the window; a newly created window gets the default size, an existing one keeps its size.
        /// </summary>
        protected static void Open<T>(string title) where T : AssetManagerWindow
        {
            bool isNew = !HasOpenInstances<T>();
            T window = GetWindow<T>(title);

            if (isNew)
                window.position = new Rect(window.position.position, new Vector2(DefaultWidth, DefaultHeight));
        }

        protected virtual void OnEnable()
        {
            EditorApplication.projectChanged += Rebuild;
        }

        protected virtual void OnDisable()
        {
            EditorApplication.projectChanged -= Rebuild;
        }

        protected override void CreateGUI()
        {
            CollectEntries();
            ApplyEntryOrder();
            ExpandEntries();
            base.CreateGUI();
            WrapInScrollView();
            AddEntrySpacing();
        }

        // Called by Alchemy whenever a field in the window changes, including when entries are reordered.
        // Entries are rebuilt from the project on every open, so only their order is persisted.
        protected override void SaveWindowData(string dataPath)
        {
            SerializedObject serializedObject = new(this);
            EntryOrder order = new();

            foreach (string propertyName in EntriesPropertyNames)
            {
                SerializedProperty entries = serializedObject.FindProperty(propertyName);
                order.Lists.Add(new EntryOrder.ListOrder { PropertyName = propertyName, Keys = GetEntryKeys(entries) });
            }

            File.WriteAllText(EntryOrderPath, JsonUtility.ToJson(order, true));
        }

        protected override void LoadWindowData(string dataPath) { }

        [Button]
        private void Refresh()
        {
            Rebuild();
        }

        private void Rebuild()
        {
            Vector2 scrollOffset = _scrollView?.scrollOffset ?? Vector2.zero;

            rootVisualElement.Clear();
            CreateGUI();
            RestoreScrollOffset(scrollOffset);
        }

        private void WrapInScrollView()
        {
            // Alchemy adds its content directly to the root, so children shrink to fit the window height
            // (default flex-shrink is 1); a ScrollView lets the content keep its natural height instead.
            VisualElement content = rootVisualElement[rootVisualElement.childCount - 1];

            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.style.flexGrow = 1f;
            _scrollView.Add(content);
            rootVisualElement.Add(_scrollView);
        }

        private void RestoreScrollOffset(Vector2 scrollOffset)
        {
            if (scrollOffset == Vector2.zero)
                return;

            // The offset is clamped to the scrollable range, which is known only after the first layout pass.
            ScrollView scrollView = _scrollView;
            scrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            void OnGeometryChanged(GeometryChangedEvent _)
            {
                scrollView.contentContainer.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                scrollView.scrollOffset = scrollOffset;
            }
        }

        /// <summary>
        /// Moves entries into the saved order; entries without a saved position keep their collected order at the end.
        /// </summary>
        private void ApplyEntryOrder()
        {
            if (!File.Exists(EntryOrderPath))
                return;

            EntryOrder order = JsonUtility.FromJson<EntryOrder>(File.ReadAllText(EntryOrderPath));
            SerializedObject serializedObject = new(this);

            foreach (string propertyName in EntriesPropertyNames)
            {
                List<string> savedKeys = order.Lists.Find(list => list.PropertyName == propertyName)?.Keys;

                if (savedKeys == null)
                    continue;

                SerializedProperty entries = serializedObject.FindProperty(propertyName);
                List<string> keys = GetEntryKeys(entries);

                // OrderBy is stable, so unknown entries (int.MaxValue) stay in their collected order.
                List<string> sortedKeys = keys
                    .OrderBy(key => savedKeys.IndexOf(key) is var index and >= 0 ? index : int.MaxValue)
                    .ToList();

                for (int target = 0; target < sortedKeys.Count; target++)
                {
                    int source = keys.IndexOf(sortedKeys[target]);
                    entries.MoveArrayElement(source, target);
                    keys.RemoveAt(source);
                    keys.Insert(target, sortedKeys[target]);
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static List<string> GetEntryKeys(SerializedProperty entries)
        {
            List<string> keys = new(entries.arraySize);

            for (int i = 0; i < entries.arraySize; i++)
                keys.Add(entries.GetArrayElementAtIndex(i).FindPropertyRelative(EntryKeyPropertyName).stringValue);

            return keys;
        }

        private void ExpandEntries()
        {
            // Foldout state is cached per object and property path, so it applies to the inspector's SerializedObject too.
            SerializedObject serializedObject = new(this);

            foreach (string propertyName in EntriesPropertyNames)
            {
                SerializedProperty entries = serializedObject.FindProperty(propertyName);

                for (int i = 0; i < entries.arraySize; i++)
                    entries.GetArrayElementAtIndex(i).isExpanded = true;
            }
        }

        private void AddEntrySpacing()
        {
            List<ListView> listViews = rootVisualElement.Query<ListView>()
                .Where(view => EntriesPropertyNames.Contains(view.bindingPath))
                .ToList();

            foreach (ListView listView in listViews)
            {
                // Alchemy builds item content in bindItem, so wrap it to style only top-level entries, not nested lists.
                // The spacing goes on the content: DynamicHeight ListView measures items without their own padding.
                Action<VisualElement, int> bindItem = listView.bindItem;
                listView.bindItem = (element, index) =>
                {
                    bindItem(element, index);

                    foreach (VisualElement content in element.Children())
                        content.style.paddingTop = EntrySpacing;
                };
            }
        }

        [Serializable]
        private sealed class EntryOrder
        {
            public List<ListOrder> Lists = new();

            [Serializable]
            public sealed class ListOrder
            {
                public string PropertyName;
                public List<string> Keys;
            }
        }
    }
}
