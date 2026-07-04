using UnityEditor;
using UnityEngine;

namespace DeiveEx.StatSystem.Editor
{
    public abstract class StatsContainerComponentInspector<T> : UnityEditor.Editor
    {
        private const string FOLDOUT_STATE_KEY = "StatsComponentComponent_Foldout";
        
        private StatsContainerComponent<T> _instance;
        
        private bool IsFoldoutOpen
        {
            get => SessionState.GetBool(FOLDOUT_STATE_KEY, false);
            set => SessionState.SetBool(FOLDOUT_STATE_KEY, value);
        }

        private void OnEnable()
        {
            _instance = (StatsContainerComponent<T>)target;

            var container = _instance.StatsContainer;
            
            if(container == null)
                return;
            
            container.StatValueChanged += UpdateUI;
            container.ModifierAdded += UpdateUI;
            container.ModifierRemoved += UpdateUI;
        }
        
        private void OnDisable()
        {
            var container = _instance.StatsContainer;
            
            if(container == null)
                return;
            
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
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
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
            }
        }
        
        private void UpdateUI(object sender, T e) => Repaint();
        private void UpdateUI(object sender, StatChangedEventArgs<T> e) => Repaint();
    }
}
