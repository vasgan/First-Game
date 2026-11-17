using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;

namespace FirstGameEditor
{
    public static class WebGlBuildValidation
    {
        private const string TemplateName = "GlowPathDefense";
        private const string TemplatePath = "Assets/WebGLTemplates/GlowPathDefense/index.html";

        [MenuItem("First Game/Validate WebGL Build")]
        public static void Validate()
        {
            if (PlayerSettings.WebGL.template != "PROJECT:" + TemplateName)
                throw new BuildFailedException(
                    "WebGL template must be set to PROJECT:GlowPathDefense in Player Settings.");

            if (!File.Exists(TemplatePath))
                throw new BuildFailedException("Missing WebGL template: " + TemplatePath);

            string template = File.ReadAllText(TemplatePath);
            Require(template, "{{{ LOADER_FILENAME }}}", "Unity loader macro");
            Require(template, "{{{ DATA_FILENAME }}}", "Unity data macro");
            Require(template, "{{{ FRAMEWORK_FILENAME }}}", "Unity framework macro");
            Require(template, "{{{ CODE_FILENAME }}}", "Unity WebAssembly macro");
            Require(template, "{{{ PRODUCT_VERSION }}}", "product-version macro");
            Require(template, "SetFullscreen(1)", "fullscreen handler");
            Require(template, "webgl-unavailable", "WebGL availability error");
            Require(template, "resize", "resize handler");

            UnityEngine.Debug.Log("FIRST_GAME_WEBGL_VALIDATION_COMPLETE");
        }

        private static void Require(string text, string expected, string label)
        {
            if (!text.Contains(expected, StringComparison.Ordinal))
                throw new BuildFailedException("WebGL template is missing " + label + ".");
        }
    }
}
