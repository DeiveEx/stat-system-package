using System.Linq;
using UnityEditor;

namespace DeiveEx.StatSystem.Editor
{
    [CustomEditor(typeof(StatsContainerComponent))]
    public class StatsContainerComponentCustomInspector : UnityEditor.Editor
    {
        private const string FOLDOUT_STATE_KEY = "StatsComponentComponent_Foldout";
        
        private StatsContainerComponent _instance;
        
        private bool IsFoldoutOpen
        {
            get => SessionState.GetBool(FOLDOUT_STATE_KEY, false);
            set => SessionState.SetBool(FOLDOUT_STATE_KEY, value);
        }

        private void OnEnable()
        {
            _instance = (StatsContainerComponent)target;

            foreach (var containerId in _instance.ContainerIds)
            {
                var container = _instance.GetStatsContainer(containerId);
                container.StatBaseValueChanged += UpdateUI;
                container.ModifierAdded += UpdateUI;
                container.ModifierRemoved += UpdateUI;
            }
        }
        
        private void OnDisable()
        {
            foreach (var containerId in _instance.ContainerIds)
            {
                var container = _instance.GetStatsContainer(containerId);
                container.StatBaseValueChanged -= UpdateUI;
                container.ModifierAdded -= UpdateUI;
                container.ModifierRemoved -= UpdateUI;
            }
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

                foreach (var stat in container.Stats)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.PrefixLabel(stat.Name);
                    EditorGUILayout.LabelField($"Current: {container.GetStat(stat.Name)} (Base: {container.GetStatBaseValue(stat.Name)})");
                    
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
