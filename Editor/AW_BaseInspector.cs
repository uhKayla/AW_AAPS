#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace ANGELWARE.AW_AAPS.Editor
{
    public class AW_BaseInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var baseInspectorUxmlPath = AssetDatabase.GUIDToAssetPath("40925a9f42ce32840a679ef041923020");
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(baseInspectorUxmlPath);

            // Instantiate UXML
            var root = visualTreeAsset.Instantiate();
            SetupContent(root);
            return root;
        }

        protected virtual void SetupContent(VisualElement root)
        {
            // Add default or custom content here
            // Example: root.Add(new Label("Override SetupContent to customize"));
        }
    }
}
#endif