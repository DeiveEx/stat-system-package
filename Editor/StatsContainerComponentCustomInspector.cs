using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DeiveEx.StatSystem.Editor
{
    [CustomEditor(typeof(StatsContainerComponent))]
    public class StatsContainerComponentCustomInspector : UnityEditor.Editor
    {
        private StatsContainerComponent _instance;
        private bool _foldout;

        private void OnEnable()
        {
            _instance = (StatsContainerComponent)target;

            foreach (var containerId in _instance.ContainerIds)
            {
                var container = _instance.GetStatsContainer(containerId);
                container.onStatBaseValueChanged += UpdateUI;
                container.onModifierAdded += UpdateUI;
                container.onModifierRemoved += UpdateUI;
            }
            
            if (Application.isPlaying)
                _foldout = true;
        }
        private void OnDisable()
        {
            foreach (var containerId in _instance.ContainerIds)
            {
                var container = _instance.GetStatsContainer(containerId);
                container.onStatBaseValueChanged -= UpdateUI;
                container.onModifierAdded -= UpdateUI;
                container.onModifierRemoved -= UpdateUI;
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            //Extending to show extra info
            _foldout = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout, "Inspect Stats");
            
            if (_foldout)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    if (!_instance.ContainerIds.Any())
                        EditorGUILayout.LabelField("No stats containers registered. Try entering Playmode.");
                    else
                        DrawStats();
                }
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawStats()
        {
            foreach (var containerId in _instance.ContainerIds)
            {
                var container = _instance.GetStatsContainer(containerId);
                EditorGUILayout.LabelField($"= Container<{containerId}>");

                foreach (var statName in container.StatNames)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.PrefixLabel(statName);
                    EditorGUILayout.LabelField($"Current: {container.GetStatCurrentValue(statName)} (Base: {container.GetStatBaseValue(statName)})");
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        private void UpdateUI(object sender, StatChangedEventArgs e)
        {
            Repaint();
        }
    }
}
