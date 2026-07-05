using UnityEditor;
using UnityEngine;

namespace DeiveEx.StatSystem.Editor
{
    public abstract class StatsContainerComponentInspector<T> : UnityEditor.Editor
    {
        private const string FOLDOUT_STATE_KEY = "StatsContainerComponent_Foldout";

        private StatsContainerComponent<T> _instance;

        private string FoldoutKey => $"{FOLDOUT_STATE_KEY}_{target.GetInstanceID()}";

        private bool IsFoldoutOpen
        {
            get => SessionState.GetBool(FoldoutKey, false);
            set => SessionState.SetBool(FoldoutKey, value);
        }

        private void OnEnable()
        {
            _instance = (StatsContainerComponent<T>)target;

            //Accessing the container outside of play mode would lazily create it in edit mode.
            //Editors are recreated when entering play mode, so OnEnable runs again and subscribes then.
            if (!Application.isPlaying)
                return;

            var container = _instance.StatsContainer;
            container.StatValueChanged += UpdateUI;
            container.ModifierAdded += UpdateUI;
            container.ModifierRemoved += UpdateUI;
        }

        private void OnDisable()
        {
            if (!Application.isPlaying || _instance == null)
                return;

            var container = _instance.StatsContainer;
            container.StatValueChanged -= UpdateUI;
            container.ModifierAdded -= UpdateUI;
            container.ModifierRemoved -= UpdateUI;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            //Extending to show extra info
            IsFoldoutOpen = EditorGUILayout.BeginFoldoutHeaderGroup(IsFoldoutOpen, "Inspect Stats");

            if (IsFoldoutOpen)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    DrawContent();
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawContent()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField("Enter play mode to display Stats");
                return;
            }

            if (_instance == null ||
                _instance.StatsContainer == null ||
                _instance.StatsContainer.Stats.Count == 0)
            {
                EditorGUILayout.LabelField("No stats to display.");
                return;
            }

            DrawStats();
        }

        private void DrawStats()
        {
            var container = _instance.StatsContainer;
            EditorGUILayout.LabelField($"= Container<{container.Id}>");

            foreach (var stat in container.Stats)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PrefixLabel(stat.ToString());
                EditorGUILayout.LabelField($"Current: {container.GetStat(stat)} (Base: {container.GetStatBaseValue(stat)})");

                EditorGUILayout.EndHorizontal();

                var modifiers = container.GetStatModifiers(stat);

                if (modifiers.Count == 0)
                    continue;

                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var modifier in modifiers)
                        EditorGUILayout.LabelField($"[{modifier.ID}] {modifier.GetDebugInfo()}");
                }
            }
        }

        private void UpdateUI(object sender, T e) => Repaint();
        private void UpdateUI(object sender, StatChangedEventArgs<T> e) => Repaint();
    }
}