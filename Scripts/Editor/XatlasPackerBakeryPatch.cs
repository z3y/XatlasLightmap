using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace z3y
{
    public class XatlasPackerBakeryPatch
    {
        const string ftBuildGraphicsPath = "Assets/Editor/x64/Bakery/scripts/ftBuildGraphics.cs";

        const string Patch0Start = "if (ftRenderLightmap.checkOverlaps)";
        const string Patch1Start = "ftUVGBufferGen.StartUVGBuffer(res, hasEmissive, bakeWithNormalMaps);";

        const string Patch0Text = "for (int i = 0; i < objsToWrite.Count(); i++)\r\n            {\r\n                var r = objsToWrite[i].GetComponent<MeshRenderer>();\r\n\r\n                if (r && r.additionalVertexStreams)\r\n                {\r\n                    objsToWriteVerticesUV2[i] = r.additionalVertexStreams.uv2;\r\n                    // this can also change\r\n                    //objsToWriteVerticesUV[i] = r.additionalVertexStreams.uv;\r\n                }\r\n            }";
        const string Patch1Text = "for (int i = 0; i < objsToWrite.Count(); i++)\r\n                {\r\n                    var r = objsToWrite[i].GetComponent<MeshRenderer>();\r\n\r\n                    if (r && r.additionalVertexStreams && r.additionalVertexStreams.uv2 != null)\r\n                    {\r\n                        objsToWriteUVOverride[i] = r.additionalVertexStreams.uv2;\r\n                    }\r\n                }";

        [MenuItem("Tools/XatlasLightmap/PatchBakery")]
        public static void PatchBakery()
        {
            if (!File.Exists(ftBuildGraphicsPath))
            {
                Debug.LogError("ftBuildGraphics.cs not found");
                return;
            }

            var lines = File.ReadLines(ftBuildGraphicsPath);

            var text = new StringBuilder();
            foreach (var line in lines)
            {

                var current = line.AsSpan().TrimStart();

                if (current.StartsWith("//BAKERY PATCH START".AsSpan(), StringComparison.Ordinal))
                {
                    Debug.LogError("Patch Already Applied");
                    goto END;
                }
                else if (current.StartsWith(Patch0Start.AsSpan(), StringComparison.Ordinal))
                {
                    text.AppendLine("//BAKERY PATCH START");
                    text.AppendLine(Patch0Text);
                    text.AppendLine("//BAKERY PATCH END");
                }
                else if (current.StartsWith(Patch1Start.AsSpan(), StringComparison.Ordinal))
                {
                    text.AppendLine("//BAKERY PATCH START");
                    text.AppendLine(Patch1Text);
                    text.AppendLine("//BAKERY PATCH END");

                }

                text.AppendLine(line);
            }

            File.WriteAllText(ftBuildGraphicsPath, text.ToString());
            AssetDatabase.Refresh();

            END:;
        }
    }
}