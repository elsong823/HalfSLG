using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ELGame
{
    public class UIViewConfigHelperWindow 
        : EditorWindow
    {
        private static List<string> contents;
        private static Rect rect;
        private static Vector2 scrollPosition;

        public static void Show(string title, List<string> contents, Rect rect)
        {
            UIViewConfigHelperWindow window = GetWindow<UIViewConfigHelperWindow>(title, true);
            UIViewConfigHelperWindow.contents = contents;
            UIViewConfigHelperWindow.rect = rect;

            window.minSize = rect.size;
            window.maxSize = rect.size;

            if (UIViewConfigHelperWindow.contents == null || UIViewConfigHelperWindow.contents.Count == 0)
                return;

            window.position = rect;
            window.Show(true);
        }

        private void OnGUI()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(rect.width), GUILayout.Height(rect.height));

            foreach (var item in contents)
            {
                GUILayout.Label(item);
            }

            GUILayout.EndScrollView();

        }

        private void OnDestroy()
        {
            contents = null;
        }
    }
}