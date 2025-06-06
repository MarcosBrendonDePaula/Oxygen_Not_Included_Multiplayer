using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.DebugTools;
using UnityEngine;

namespace ONI_MP
{
        public static class Utils
        {
            /// <summary>
            /// Recursively logs the hierarchy of a GameObject and its children.
            /// </summary>
            /// <param name="root">The root transform to start logging from.</param>
            /// <param name="prefix">Used internally for indentation.</param>
            public static void LogHierarchy(Transform root, string prefix = "")
            {
                if (root == null)
                {
                    DebugConsole.LogWarning("LogHierarchy called with null root.");
                    return;
                }

                DebugConsole.Log($"{prefix}{root.name}");

                foreach (Transform child in root)
                {
                    LogHierarchy(child, prefix + "  ");
                }
            }
        }
    }
