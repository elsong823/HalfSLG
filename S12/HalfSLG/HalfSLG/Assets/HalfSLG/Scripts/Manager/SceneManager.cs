using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ELGame
{
    public class SceneManager 
        : BaseManager<SceneManager>
    {
        public override string MgrName => "SceneManager";

        public void LoadSceneAsync(string sceneName, System.Action<float> updateProgross, System.Action<string> afterCallback)
        {
            if (Resource.ResourceManager.Instance.editorMode)
            {
                StartCoroutine(LoadScene(sceneName, updateProgross, afterCallback));
                return;
            }

            AssetBundle sceneBundle = Resource.ResourceManager.Instance.GetAssetBundle(string.Format("scenes/{0}.unity3d", sceneName), sceneName);
            if (sceneBundle != null)
            {
                string[] paths = sceneBundle.GetAllScenePaths();
                string path = string.Empty;
                for (int i = 0; i < paths.Length; ++i)
                {
                    if (paths[i].ToLower().Contains(sceneName.ToLower()))
                    {
                        path = paths[i];
                        break;
                    }
                }
                if (string.IsNullOrEmpty(path))
                    StartCoroutine(LoadScene(sceneName, updateProgross, afterCallback));
                else
                    StartCoroutine(LoadScene(path, updateProgross, afterCallback));
            }
        }

        IEnumerator LoadScene(string sceneName, System.Action<float> updateProgross, System.Action<string> afterCallback)
        {
            var async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            while (!async.isDone)
            {
                if (updateProgross != null)
                    updateProgross(async.progress);
                yield return null;
            }

            //要再等一帧
            yield return null;

            if (afterCallback != null)
                afterCallback(sceneName);
        }
    }
}