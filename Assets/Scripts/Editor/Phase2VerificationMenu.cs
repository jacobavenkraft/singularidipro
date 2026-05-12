#nullable enable
using Singularidi.Unity.Audio;
using Singularidi.Unity.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Singularidi.Editor
{
    // Discoverability anchor for the Phase 2 verification rig.
    //
    // The committed MainScene.unity ships with a wired-up PlaybackHost GameObject. If the rig
    // is missing or damaged (a future developer cleared the scene, a merge conflict ate the
    // YAML, etc.) this menu rebuilds it from scratch. It also serves as the discoverability
    // entry-point: a developer 10 years from now can scan the menu bar, find
    // "Singularidi/Phase 2/Rebuild Verification Rig", and recover the canonical setup.
    internal static class Phase2VerificationMenu
    {
        private const string CameraName = "Main Camera";
        private const string HostName = "PlaybackHost";

        [MenuItem("Singularidi/Phase 2/Rebuild Verification Rig")]
        private static void RebuildRig()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[Phase2VerificationMenu] No active scene.");
                return;
            }

            DestroyExisting(CameraName);
            DestroyExisting(HostName);

            var cameraGo = new GameObject(CameraName, typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0, 1, -10);
            var cam = cameraGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0f);
            cam.allowHDR = true;
            cam.allowMSAA = false; // SMAA via URP per Phase 0 note

            var hostGo = new GameObject(HostName);
            var audioSource = hostGo.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.bypassEffects = true;
            audioSource.bypassListenerEffects = true;
            audioSource.bypassReverbZones = true;
            hostGo.AddComponent<MeltySynthAudioEngine>();
            hostGo.AddComponent<PlaybackHost>();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Phase2VerificationMenu] Rebuilt rig in scene '" + scene.name + "'. Save the scene to persist.");
        }

        private static void DestroyExisting(string objectName)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null)
                Object.DestroyImmediate(existing);
        }
    }
}
