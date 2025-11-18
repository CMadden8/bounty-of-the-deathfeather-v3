using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CombatPOC.Editor
{
    /// <summary>
    /// Editor utility to ensure the Main Camera has a PhysicsRaycaster component.
    /// Required for detecting mouse clicks on units and cells in 3D scenes.
    /// </summary>
    public static class EnsureCameraRaycaster
    {
        private const string MenuPath = "Tools/Combat POC/Ensure Camera Raycaster";

        [MenuItem(MenuPath)]
        public static void EnsureRaycaster()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                EditorUtility.DisplayDialog("Camera Not Found", 
                    "No Main Camera found in the scene. Please tag your camera as 'MainCamera'.", 
                    "OK");
                return;
            }

            // Check for PhysicsRaycaster
            var raycaster = mainCamera.GetComponent<PhysicsRaycaster>();
            if (raycaster == null)
            {
                raycaster = mainCamera.gameObject.AddComponent<PhysicsRaycaster>();
                EditorUtility.SetDirty(mainCamera.gameObject);
                Debug.Log($"Added PhysicsRaycaster to {mainCamera.name}");
            }
            else
            {
                Debug.Log($"PhysicsRaycaster already present on {mainCamera.name}");
            }

            // Ensure EventSystem exists in scene
            var eventSystem = Object.FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystem = eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
                Debug.Log("Created EventSystem GameObject");
            }
            else
            {
                Debug.Log("EventSystem already present in scene");
            }

            EditorUtility.DisplayDialog("Camera Raycaster Check", 
                "Camera setup complete:\n\n" +
                $"• PhysicsRaycaster on {mainCamera.name}\n" +
                "• EventSystem in scene\n\n" +
                "Your camera can now detect clicks on units and cells.", 
                "OK");
        }

        [MenuItem(MenuPath, true)]
        public static bool ValidateEnsureRaycaster()
        {
            return !Application.isPlaying;
        }
    }
}
