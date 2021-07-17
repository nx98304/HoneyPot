using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Linq;
using Character;
using IllusionPlugin;
using HarmonyLib;
using Studio;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Diagnostics;

namespace ClassLibrary4
{
    public class HoneyPot : MonoBehaviour
    {
        private void Awake()
        {
            //Note: so this has been lacking all this time.. making HoneyPot object kept being re-created.
            DontDestroyOnLoad(transform.gameObject);
        }

        private void Start()
        {
            self = GameObject.Find("HoneyPot").GetComponent<HoneyPot>();
            string str = ModPrefs.GetString("HoneyPot", "ForceColor", "", false);
            if( str.Equals("TRUE") )
            {
                force_color_everything_that_doesnt_have_materialcustoms = true;
            }
        }

        #region important helpers (RQ, MaterialCustoms parameter remapping)
        //Generalized Render Queue retrival: SRQ MB will have the last say if it is present, 
        //                                   otherwise use the CustomRenderQueue value. 
        private int getRenderQueue(string inspector_key, SetRenderQueue setRQ_MB)
        {
            int result = -1; //is this default value good?
            if (HoneyPot.material_rq.ContainsKey(inspector_key))
            {
                result = HoneyPot.material_rq[inspector_key];
            }

            if (setRQ_MB != null)
            {
                int[] array = setRQ_MB.Get();
                if (array.Length != 0)
                {
                    result = array[0];
                }
            }
            //this.logSave("set RQ: " + inspector_key + " = " + result);
            return result;
        }

        Dictionary<string, string[]> MC_Mapping = new Dictionary<string, string[]>()
        {
            { "Shader Forge/PBR_SG",            new string[] { "_MainColor", "_SpecularColor", "_Specular", "_Gloss", "_not_mapped_", "_not_mapped_", "_not_mapped_" } },
            { "Shader Forge/PBR_SG Alpha",      new string[] { "_MainColor", "_SpecularColor", "_Specular", "_Gloss", "_not_mapped_", "_not_mapped_", "_not_mapped_" } },
            { "Shader Forge/PBR_SG DoubleSide", new string[] { "_MainColor", "_SpecularColor", "_Specular", "_Gloss", "_not_mapped_", "_not_mapped_", "_not_mapped_" } },
            { "Shader Forge/PBR_SG Clip",       new string[] { "_MainColor", "_SpecularColor", "_Specular", "_Gloss", "_not_mapped_", "_not_mapped_", "_not_mapped_" } },
            { "Shader Forge/PBRsp_2layer",      new string[] { "_Color", "_SpecColor", "_Metallic", "_Smoothness", "_Color_2", "_SpecColor_2", "_SpecColor_2" } },
            { "Standard",                       new string[] { "_Color", "_not_mapped_", "_Metallic", "_Glossiness", "_not_mapped_", "_not_mapped_", "_not_mapped_" } },
            { "Standard_Z",                     new string[] { "_Color", "_not_mapped_", "_Metallic", "_Glossiness", "_not_mapped_", "_not_mapped_", "_not_mapped_" } },
            { "Standard CustomMetallic",        new string[] { "_Color", "_SpecularColor", "_Metallic", "_Glossiness", "_not_mapped_", "_not_mapped_", "_not_mapped_" } },
            { "Standard_culloff",               new string[] { "_Color", "_not_mapped_", "_Metallic", "_Glossiness", "_not_mapped_", "_not_mapped_", "_not_mapped_" } },
            { "Standard_culloff_Z",             new string[] { "_Color", "_not_mapped_", "_Metallic", "_Glossiness", "_not_mapped_", "_not_mapped_", "_not_mapped_" } },
            { "Standard (Specular setup)",      new string[] { "_Color", "_SpecColor", "_not_mapped_", "_Glossiness", "_not_mapped_", "_not_mapped_", "_not_mapped_" } },
            { "Standard (Specular setup)_culloff", new string[] { "_Color", "_SpecColor", "_not_mapped_", "_Glossiness", "_not_mapped_", "_not_mapped_", "_not_mapped_" } },
            { "Standard_555",                   new string[] { "_Color", "_not_mapped_", "_Metallic", "_Glossiness", "_not_mapped_", "_not_mapped_", "_not_mapped_" } },
            { "HSStandard",                     new string[] { "_Color", "_SpecColor", "_Metallic", "_Smoothness", "_not_mapped_", "_not_mapped_", "_not_mapped_" } },
            { "HSStandard (Two Colors)",        new string[] { "_Color", "_SpecColor", "_Metallic", "_Smoothness", "_Color_3", "_SpecColor_3", "_SpecColor_3" } },
            { "Alloy/Core",                     new string[] { "_Color", "_not_mapped_", "_Metal", "_Roughness", "_not_mapped_", "_not_mapped_", "_not_mapped_" } },
            { "Shader Forge/Hair/ShaderForge_Hair", new string[] { "_Color", "_CuticleColor", "_CuticleExp", "_CuticleY", "_FrenelColor", "_not_mapped_", "_FrenelExp" } },
        };                                    //Note: the 7th idx is actually manipulating 6th's alpha value it seems, so only ones that runs TWO colors would need it...

        private static FieldInfo MC_DataFloat_minField = typeof(MaterialCustoms).GetNestedType("Data_Float").GetField("min", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo MC_DataFloat_maxField = typeof(MaterialCustoms).GetNestedType("Data_Float").GetField("max", BindingFlags.NonPublic | BindingFlags.Instance);

        private void match_correct_shader_property(MaterialCustoms mc, string shader_name)
        {
            if ( shader_name.IsNullOrEmpty() ) return;

            string key = "";
            if (MC_Mapping.ContainsKey(shader_name))                  key = shader_name;
            else if (shader_name.Contains("HSStandard (Two Colors)")) key = "HSStandard (Two Colors)";
            else if (shader_name.Contains("HSStandard"))              key = "HSStandard";
            else if (shader_name.Contains("PBRsp_2layer"))            key = "Shader Forge/PBRsp_2layer";
            else if (shader_name.Contains("ShaderForge_Hair"))        key = "Shader Forge/Hair/ShaderForge_Hair";

            if( !key.IsNullOrEmpty() )
                for( int idx = 0; idx < MC_Mapping[key].Length; idx++ )
                    mc.parameters[idx].propertyName = MC_Mapping[key][idx];
            
            if( key == "Shader Forge/Hair/ShaderForge_Hair" )
            {
                mc.parameters[2].type = MaterialCustoms.Parameter.TYPE.FLOAT11;
                mc.parameters[3].type = MaterialCustoms.Parameter.TYPE.FLOAT11;
                mc.parameters[6].type = MaterialCustoms.Parameter.TYPE.FLOAT11;
            }
        }

        private void match_correct_shader_property_data_range(MaterialCustoms mc, string shader_name)
        {
            if (shader_name.Contains("ShaderForge_Hair"))
            {
                MC_DataFloat_minField.SetValue(mc.datas[2], 1);
                MC_DataFloat_maxField.SetValue(mc.datas[2], 20);
                MC_DataFloat_minField.SetValue(mc.datas[6], 0);
                MC_DataFloat_maxField.SetValue(mc.datas[6], 8);
            }
        }
        #endregion

        #region Any shader remapping that's material and texture only
        private static FieldInfo head_humanField = typeof(Head).GetField("human", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void Head_ChangeEyebrow_Postfix(Head __instance)
        {
            Human h = head_humanField.GetValue(__instance) as Human;
            if (HoneyPot.idFileDict.ContainsKey(h.customParam.head.eyeBrowID) && HoneyPot.presets.ContainsKey("PBRsp_texture_alpha"))
            {
                h.head.Rend_eyebrow.material.shader = HoneyPot.presets["PBRsp_texture_alpha"].shader;
            }
        }

        //Note: You can't use Harmony Annotations with this because "Postfix(Head __instance)" is ambigious.
        private static void Head_ChangeEyelash_Postfix(Head __instance)
        {
            Human h = head_humanField.GetValue(__instance) as Human;
            if (HoneyPot.idFileDict.ContainsKey(h.customParam.head.eyeLashID) && HoneyPot.presets.ContainsKey("PBRsp_texture_alpha_culloff"))
            {
                h.head.Rend_eyelash.material.shader = HoneyPot.presets["PBRsp_texture_alpha_culloff"].shader;
            }
        }
        #endregion

        #region Hairs shader remapping
        private static FieldInfo Hairs_humanField = typeof(Hairs).GetField("human", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPatch(typeof(Hairs), "Load")]
        [HarmonyPostfix]
        private static void Postfix(Hairs __instance, HairParameter param)
        {
            SEX sex = (Hairs_humanField.GetValue(__instance) as Human).sex;
            for (int i = 0; i < 3; i++)
            {
                if (__instance.objHairs[i] != null)
                {
                    int id = param.parts[i].ID;
                    HairData hair_data = (sex == SEX.FEMALE) ?
                        CustomDataManager.GetHair_Female((HAIR_TYPE)i, id) :
                        CustomDataManager.GetHair_Male(id);
                    self.setHairShaderObj(__instance.objHairs[i], hair_data.assetbundleName.Replace("\\", "/"));
                }
            }
            __instance.ChangeColor(param);
        }

        public void setHairShaderObj(GameObject objHair, string assetBundleName)
        {
            try
            {
                Renderer[] renderers = objHair.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    foreach (Material material in r.materials)
                    {
                        if (HoneyPot.orgShader == null && !"".Equals(material.shader.name))
                        {
                            HoneyPot.orgShader = material.shader;
                        }
                        if (HoneyPot.orgShader != null && "".Equals(material.shader.name))
                        {
                            string inspector_key = (assetBundleName + "|" + material.name).Replace(" (Instance)", "");
                            this.logSave("Hair material: " + inspector_key);
                            int rq = material.renderQueue;
                            if (HoneyPot.inspector.ContainsKey(inspector_key))
                            {
                                rq = getRenderQueue(inspector_key, r.gameObject.GetComponent<SetRenderQueue>());
                                string shader_name = HoneyPot.inspector[inspector_key];
                                //Note: So, for HS hairs, ideally we want to convert all of them to PH hair shader
                                //      because that's usually just better, but hairs that use "HSStandard" is an exception.
                                if (HoneyPot.presets.ContainsKey(shader_name) && HoneyPot.presets[shader_name].shader.name.Contains("HSStandard"))
                                {
                                    material.shader = HoneyPot.presets[shader_name].shader;
                                    this.logSave("shader: " + HoneyPot.inspector[inspector_key] + " ==> " + material.shader.name);

                                    this.logSave(" - HSStandard shader family detected for Hairs, trying to assign RenderType...");
                                    this.logSave("  (Rendering) Mode: " + material.GetFloat("_Mode"));
                                    bool isAlphaTest = material.IsKeywordEnabled("_ALPHATEST_ON");
                                    bool isAlphaPremultiply = material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");
                                    bool isAlphaBlend = material.IsKeywordEnabled("_ALPHABLEND_ON");

                                    this.logSave("  (TEST, PREMULTIPLY, BLEND) = " + isAlphaTest + "," + isAlphaPremultiply + "," + isAlphaBlend);

                                    if (isAlphaTest) material.SetOverrideTag("RenderType", "TransparentCutout");
                                    if (isAlphaPremultiply) material.SetOverrideTag("RenderType", "Transparent");
                                    if (isAlphaBlend) material.SetOverrideTag("RenderType", "Transparent");

                                    this.logSave("  RenderType: " + material.GetTag("RenderType", false));
                                }
                                else {
                                    if (rq <= 2500)
                                    {
                                        material.shader = HoneyPot.presets["Standard"].shader;
                                        this.logSave("This part of the hair mod seems to be non-transparent: " + HoneyPot.inspector[inspector_key] + ", default to " + HoneyPot.presets["Standard"].shader.name);
                                        this.logSave("  (Rendering) Mode: " + material.GetFloat("_Mode"));
                                        bool isAlphaTest = material.IsKeywordEnabled("_ALPHATEST_ON");
                                        bool isAlphaPremultiply = material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");
                                        bool isAlphaBlend = material.IsKeywordEnabled("_ALPHABLEND_ON");

                                        this.logSave("  (TEST, PREMULTIPLY, BLEND) = " + isAlphaTest + "," + isAlphaPremultiply + "," + isAlphaBlend);

                                        if (isAlphaTest) material.SetOverrideTag("RenderType", "TransparentCutout");
                                        if (isAlphaPremultiply) material.SetOverrideTag("RenderType", "Transparent");
                                        if (isAlphaBlend) material.SetOverrideTag("RenderType", "Transparent");

                                        this.logSave("  RenderType: " + material.GetTag("RenderType", false));
                                    }
                                    else
                                    {
                                        // TODO: We know PH hair shader is better. but we also want to distinguish culloff vs not culloff hair
                                        //       but how do we do that here?
                                        this.logSave("We know the hair shader is " + HoneyPot.inspector[inspector_key] + ", but PH's hair shader is almost always better: " + HoneyPot.orgShader.name);
                                        material.shader = HoneyPot.orgShader;
                                    }
                                }
                            }
                            else
                            {
                                // catch all using the standard PH hair shader.
                                this.logSave("If we got here, it means this hair should be PH hair, but we failed to read its shader. Apply default PH hair shader: " + HoneyPot.orgShader.name);
                                material.shader = HoneyPot.orgShader;
                            }
                            material.renderQueue = rq;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.logSave(ex.ToString());
            }
        }
        #endregion

        #region Studio item shader remapping 
        private static void AddObjectItem_Load_Postfix(OCIItem __result)
        {
            if (__result == null)
            {
                Console.WriteLine("HoneyPot detected invalid Studio items.");
                return;
            }

            if (Singleton<Info>.Instance.dicItemLoadInfo.ContainsKey(__result.itemInfo.no))
            {
                Info.ItemLoadInfo itemLoadInfo = Singleton<Info>.Instance.dicItemLoadInfo[__result.itemInfo.no];
                self.setItemShader(__result.objectItem, itemLoadInfo.bundlePath.Replace("\\", "/"));
                if (__result.isColor2 || __result.isChangeColor)
                {
                    __result.UpdateColor();
                }
            }
            else
            {
                Console.WriteLine("HoneyPot detected invalid item loadInfo. Maybe some item ID changed when that item was originally saved into the scene.");
            }
        }

        public void setItemShader(GameObject obj, string fileName)
        {
            Renderer[] renderers_in_children = obj.GetComponentsInChildren<Renderer>(true);
            Projector[] projectors_in_children = obj.GetComponentsInChildren<Projector>(true);
            foreach (Projector p in projectors_in_children)
            {
                // test
                p.material.shader = HoneyPot.presets["Particle Add"].shader;
            }
            foreach (Renderer r in renderers_in_children)
            {
                Type renderertype = r.GetType();
                if (renderertype == typeof(ParticleSystemRenderer) ||
                    renderertype == typeof(LineRenderer) ||
                    renderertype == typeof(TrailRenderer) ||
                    renderertype == typeof(ParticleRenderer))
                {
                    this.logSave(r.name + " is probably an effects renderer, needs special guesses.");
                    Material particle_mat = r.materials[0]; // assume one particle renderer only uses 1 material.
                    this.logSave("particles!");
                    this.logSave("material:" + fileName + "|" + particle_mat.name);

                    string shader_name = "";
                    string inspector_key = fileName + "|" + particle_mat.name.Replace(" (Instance)", "");

                    if (HoneyPot.inspector.ContainsKey(inspector_key))
                    {
                        shader_name = HoneyPot.inspector[inspector_key];
                        if (shader_name.Length == 0)
                        {
                            this.logSave("HoneyPotInspector.txt have record of this PARTICLE material, but failed to read its original shader name, likely the shader used by this prefab was not present in the assetbundle. ");
                        }
                    }
                    else
                    {
                        this.logSave("HoneyPotInspector.txt have no record of this PARTICLE material. Regenerate HoneyPotInspector.txt is recommended, but it rarely helps with particle effects.");
                    }

                    this.logSave("shader_name:" + shader_name);
                    if (shader_name.Length > 0)
                    {
                        if (HoneyPot.presets.ContainsKey(shader_name))
                        {
                            particle_mat.shader = HoneyPot.presets[shader_name].shader;
                        }
                        else
                        {
                            this.logSave("The preset shaders weren't prepared for this specific HS particle shader. Likely it was a custom shader. Which we can only guess from now on.");
                            bool is_probably_add = false;
                            bool is_probably_blend = false;
                            bool is_probably_opaque = false;
                            foreach (string s in particle_mat.shaderKeywords)
                            {
                                if (s.Contains("ADD") || s.Contains("Add") || s.Contains("add")) // given ADD is the most usual type, giving it highest priority
                                {
                                    is_probably_add = true;
                                }
                                else if (s.Contains("BLEND") || s.Contains("Blend") || s.Contains("blend"))
                                {
                                    is_probably_blend = true;
                                }
                                else if (s.Contains("NORMAL") || s.Contains("Normal") || s.Contains("normal"))
                                {
                                    is_probably_opaque = true; // like small flying rocks
                                }
                            }

                            if (shader_name.Contains("Distortion"))
                            {
                                this.logSave("We should try to import a Distorion effect to PH to deal with this. Right now let's just use simple particle effect shader.");
                                particle_mat.shader = HoneyPot.presets["Particle Add"].shader;
                            }
                            else if (is_probably_add || shader_name.Contains("Add") || shader_name.Contains("add"))
                            {
                                particle_mat.shader = HoneyPot.presets["Particle Add"].shader;
                            }
                            else if (is_probably_blend || shader_name.Contains("Blend") || shader_name.Contains("blend"))
                            {
                                particle_mat.shader = HoneyPot.presets["Particle Alpha Blend"].shader;
                            }
                            else if (is_probably_opaque || shader_name.Contains("Cutout") || shader_name.Contains("Diffuse"))
                            {
                                particle_mat.shader = HoneyPot.presets["Standard"].shader;
                            }
                            else
                            {
                                particle_mat.shader = HoneyPot.presets["Particle Add"].shader; // catch-all for particles
                            }
                        }
                    }
                    else
                    {
                        this.logSave("Inspector failed to resolve the particle shader name from HS. Which is entirely normal -- however we are going to try guessing what it should map to.");
                        particle_mat.shader = HoneyPot.presets["Particle Add"].shader; // catch-all for particles
                    }
                    this.logSave("shader:" + particle_mat.shader.name);
                    this.logSave("-- end of one material processing --");
                }
                else
                {
                    foreach (Material material in r.materials)
                    {
                        string shader_name = "";
                        string inspector_key = fileName + "|" + material.name.Replace(" (Instance)", "");
                        int guessing_renderqueue = getRenderQueue(inspector_key, r.gameObject.GetComponent<SetRenderQueue>());
                        if ("".Equals(material.shader.name))
                        {
                            this.logSave("item!");
                            this.logSave("material:" + inspector_key);

                            if (HoneyPot.inspector.ContainsKey(inspector_key))
                            {
                                shader_name = HoneyPot.inspector[inspector_key];
                                if (shader_name.Length == 0)
                                {
                                    this.logSave("HoneyPotInspector.txt have record of this material, but failed to read its shader name or PathID.");
                                }
                            }
                            else
                            {
                                this.logSave(inspector_key + " not found in HoneyPotInspector.txt. Resort to default (usually means you have to regenerate HoneyPotInspector.txt)");
                            }

                            this.logSave("shader_name:" + shader_name);

                            if (shader_name.Contains_NoCase("distortion"))
                            {
                                this.logSave("We should try to import a Distorion effect to PH to deal with this. Right now let's just use simple particle effect shader.");
                                shader_name = "Particle Add";
                                material.shader = HoneyPot.presets[shader_name].shader;
                                if (guessing_renderqueue == -1) guessing_renderqueue = 4123;
                            }
                            else if (shader_name.Contains_NoCase("alphatest"))
                            {
                                if (guessing_renderqueue <= 2500)
                                {
                                    this.logSave("An AlphaTest kind of shader that has non-transparent renderqueue, assigning PBRsp_alpha_culloff");
                                    shader_name = "PBRsp_alpha_culloff";
                                }
                                else
                                {
                                    this.logSave("An AlphaTest kind of shader that has transparent renderqueue, assigning PBRsp_3mask_alpha");
                                    shader_name = "PBRsp_3mask_alpha";
                                }
                                material.shader = HoneyPot.presets[shader_name].shader;
                            }
                            else
                            {
                                if (!HoneyPot.presets.ContainsKey(shader_name))
                                {
                                    this.logSave("Shader remapping info not found from shader.txt for this Studio item. Testing a few shader keywords to salvage.");
                                    foreach (string text2 in material.shaderKeywords)
                                    {
                                        this.logSave("shader keywords found:" + text2);
                                        if ((text2.Contains_NoCase("alphapre") || material.name.Contains_NoCase("glass")) &&
                                           !(text2.Contains_NoCase("leaf") || text2.Contains_NoCase("frond") || text2.Contains_NoCase("branch"))) //super last-ditch tests for glass-like material
                                        {
                                            this.logSave("Possible transparent glasses-like material.");
                                            shader_name = "Standard";
                                            material.shader = HoneyPot.presets[shader_name].shader;
                                            // more hacking for HS glass shaders that I know of. 
                                            if (material.HasProperty("_Glossiness"))
                                            {
                                                float glossiness = material.GetFloat("_Glossiness");
                                                //glossiness += (1.0f-glossiness) / 2;
                                                material.SetFloat("_Glossiness", glossiness);
                                            }
                                            if (material.HasProperty("_DstBlend"))
                                            {
                                                float dstblend = material.GetFloat("_DstBlend");
                                                if (dstblend < 1.0f) material.SetFloat("_DstBlend", 1.0f);
                                            }
                                            if (material.HasProperty("_ZWrite"))
                                            {
                                                float zwrite = material.GetFloat("_ZWrite");
                                                if (zwrite > 0.0f) material.SetFloat("_ZWrite", 0.0f);
                                            }
                                            if (material.HasProperty("_Color"))
                                            {
                                                Color c = material.GetColor("_Color");
                                                //c.r *= c.a;
                                                //c.g *= c.a;
                                                //c.b *= c.a;
                                                if (c.a < 0.3f) c.a = 0.3f;
                                                material.SetColor("_Color", c);
                                            }

                                            // guessing any glass like item should have a very high render queue;
                                            // in this case anything you can get from getRenderQueue() is probably wrong and not suitable.
                                            if (guessing_renderqueue == -1) guessing_renderqueue = 4001;
                                        }
                                        else if (text2.Contains_NoCase("trans") || text2.Contains_NoCase("blend"))
                                        {
                                            this.logSave("Possible unspecified transparent material.");
                                            shader_name = "PBRsp_3mask_alpha";
                                            material.shader = HoneyPot.presets[shader_name].shader;
                                            // another guess, but make the number different so it's easier to tell.
                                            if (guessing_renderqueue == -1) guessing_renderqueue = 3123;
                                        }
                                        else if (text2.Contains_NoCase("alphatest") || text2.Contains_NoCase("leaf") || text2.Contains_NoCase("frond") || text2.Contains_NoCase("branch"))
                                        {
                                            this.logSave("Possible plant / tree / leaf / branch -like materials.");
                                            shader_name = "PBRsp_alpha_culloff";
                                            material.shader = HoneyPot.presets[shader_name].shader;
                                            // in this case, you can probably rely on getRenderQueue() results.
                                        }
                                    }
                                }
                                else
                                {
                                    this.logSave("Shader remapping info found, remapping " + shader_name + " to " + HoneyPot.presets[shader_name].shader.name);
                                    material.shader = HoneyPot.presets[shader_name].shader;
                                    if (material.shader.name.Contains("HSStandard"))
                                    {
                                        this.logSave(" - HSStandard shader family detected for clothing, trying to assign RenderType...");
                                        this.logSave("  (Rendering) Mode: " + material.GetFloat("_Mode"));
                                        bool isAlphaTest = material.IsKeywordEnabled("_ALPHATEST_ON");
                                        bool isAlphaPremultiply = material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");
                                        bool isAlphaBlend = material.IsKeywordEnabled("_ALPHABLEND_ON");

                                        this.logSave("  (TEST, PREMULTIPLY, BLEND) = " + isAlphaTest + "," + isAlphaPremultiply + "," + isAlphaBlend);

                                        if (isAlphaTest) material.SetOverrideTag("RenderType", "TransparentCutout");
                                        if (isAlphaPremultiply) material.SetOverrideTag("RenderType", "Transparent");
                                        if (isAlphaBlend) material.SetOverrideTag("RenderType", "Transparent");

                                        this.logSave("  RenderType: " + material.GetTag("RenderType", false));
                                    }
                                }
                            }

                            if (!HoneyPot.presets.ContainsKey(shader_name))
                            {
                                this.logSave("The preset shaders weren't prepared for this specific HS shader, and all guessing for shader keywords have failed, resorting to default.");
                                material.shader = HoneyPot.presets["Standard"].shader;
                                if (material.HasProperty("_Glossiness"))
                                {
                                    float glossiness = material.GetFloat("_Glossiness");
                                    if (glossiness > 0.2f)
                                    {   // It does seem like the standard shader of PH is somewhat too glossy
                                        // when it is used as a fallback shader.
                                        this.logSave("PH Standard tends to have higher gloss than usual for materials/shaders from HS. Try to lower it here.");
                                        material.SetFloat("_Glossiness", 0.2f);
                                    }
                                }
                            }

                            if (material.HasProperty("_Glossiness") && material.HasProperty("_Color"))
                            {
                                Color c = material.GetColor("_Color");
                                this.logSave(" - Monitor this material's values, see if it is reset or has anomoly: ");
                                this.logSave(" --- _Glossiness: " + material.GetFloat("_Glossiness"));
                                this.logSave(" ---       color: (" + c.r + "," + c.g + "," + c.b + "," + c.a + ")");
                            }
                            material.renderQueue = guessing_renderqueue;
                            this.logSave("final shader:" + material.shader.name + ", final RQ = " + material.renderQueue);
                            this.logSave("-- end of one material processing --");
                        }
                    }
                }
            }
        }
        #endregion

        #region Accessory shader remapping
        [HarmonyPatch(typeof(Accessories), "AccessoryInstantiate")]
        [HarmonyPostfix]
        private static void Postfix(Accessories __instance, AccessoryParameter acceParam, int slot, bool fixAttachParent, AccessoryData prevData)
        {   //Note: this is required because we removed the UpdateColorCustom right at the end of SetupMaterials 
            //      with the transpiler below
            __instance.UpdateColorCustom(slot);
            self.setAccsShader(__instance, acceParam, slot);
        }

        private static IEnumerable<CodeInstruction> AcceObj_SetupMaterials_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int startIndex = -1;
            int endIndex = codes.Count - 1;
            for (int i = endIndex; i >= 0; i--)
            {
                if (codes[i].opcode == OpCodes.Call)
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes[startIndex - 1].opcode = OpCodes.Nop;
                codes[startIndex].opcode = OpCodes.Nop;
                // Remove the UpdateColorCustom call at the end of AcceObj.SetupMaterials
            }
            return codes.AsEnumerable();
        }

        private static MethodInfo AcceObj_SetupMaterials =
            Assembly.GetAssembly(typeof(Accessories)).GetType("Accessories+AcceObj").GetMethod("SetupMaterials", new Type[] { typeof(AccessoryData) });

        private static void AcceObj_UpdateColorCustom_Prefix(object __instance)
        {
            AcceObj_SetupMaterials.Invoke(__instance, new object[] { null });
        }

        private static MethodInfo MaterialCustoms_Setup = typeof(MaterialCustoms).GetMethod("Setup", new Type[0]);

        [HarmonyPatch(typeof(MaterialCustoms), "Setup")]
        [HarmonyPrefix]
        private static bool Prefix(MaterialCustoms __instance)
        {
            if (__instance == null || __instance.parameters == null)
                return false;

            Renderer[] componentsInChildren = __instance.GetComponentsInChildren<Renderer>(true);
            __instance.datas = new MaterialCustoms.Data_Base[__instance.parameters.Length];
            for (int i = 0; i < __instance.parameters.Length; i++)
            {
                MaterialCustoms.Parameter parameter = __instance.parameters[i];
                if (parameter == null)
                    return false;

                if (parameter.type == MaterialCustoms.Parameter.TYPE.FLOAT01)
                {
                    __instance.datas[i] = new MaterialCustoms.Data_Float(parameter, componentsInChildren, 0f, 1f);
                }
                else if (parameter.type == MaterialCustoms.Parameter.TYPE.FLOAT11)
                {
                    __instance.datas[i] = new MaterialCustoms.Data_Float(parameter, componentsInChildren, -1f, 1f);
                }
                else if (parameter.type == MaterialCustoms.Parameter.TYPE.COLOR)
                {
                    __instance.datas[i] = new MaterialCustoms.Data_Color(parameter, componentsInChildren);
                }
                else if (parameter.type == MaterialCustoms.Parameter.TYPE.ALPHA)
                {
                    __instance.datas[i] = new MaterialCustoms.Data_Alpha(parameter, componentsInChildren);
                }
            }
            return false;
        }

        private static FieldInfo Accessories_acceObjsField = typeof(Accessories).GetField("acceObjs", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo AcceObj_objField = Assembly.GetAssembly(typeof(Accessories)).GetType("Accessories+AcceObj").GetField("obj");

        public void setAccsShader(Accessories acce, AccessoryParameter acceParam, int slot)
        {
            AccessoryCustom acceCustom = acceParam.slot[slot];
            AccessoryData accessoryData = CustomDataManager.GetAcceData(acceCustom.type, acceCustom.id);
            if (accessoryData == null)
            {
                return;
            }
            GameObject acceobj_obj = null;
            try
            {   //Note: WHAT THE FUCK ILLUSION Accessories.objAcs getter by itself would not work without a patch
                acceobj_obj = acce.objAcs[slot];
            }
            catch
            {   //Note: Have to catch here to use the old method when MoreAccessoriesPH isn't in place.
                object[] acceobjs = Accessories_acceObjsField.GetValue(acce) as object[];
                if (slot < 10)
                {
                    acceobj_obj = AcceObj_objField.GetValue(acceobjs[slot]) as GameObject;
                }
                else
                {
                    Console.WriteLine("Honeypot detects attempts to load accessory slot > 10, but MoreAccessoriesPH isn't in place. Ignored this accessory.");
                    return;
                }
            }
            Renderer[] renderers_in_acceobj = acceobj_obj.GetComponentsInChildren<Renderer>(true);
            string try_this_shader_name = "";
            List<string> list = new List<string>();
            foreach (Renderer r in renderers_in_acceobj)
            {
                foreach (Material material in r.materials)
                {
                    string material_name = material.name.Replace(" (Instance)", "");
                    string inspector_key = accessoryData.assetbundleName.Replace("\\", "/") + "|" + material_name;

                    if (material.shader.name.IsNullOrEmpty()) //This is the only way we know it's from HS.
                    {
                        this.logSave("Acce material: " + inspector_key);
                        int rq = material.renderQueue;
                        if (HoneyPot.inspector.ContainsKey(inspector_key))
                        {
                            rq = getRenderQueue(inspector_key, r.gameObject.GetComponent<SetRenderQueue>());
                            if (HoneyPot.presets.ContainsKey(HoneyPot.inspector[inspector_key]))
                            {
                                material.shader = HoneyPot.presets[HoneyPot.inspector[inspector_key]].shader;
                                this.logSave("shader: " + HoneyPot.inspector[inspector_key] + " ==> " + material.shader.name);
                                if (material.shader.name.Contains("HSStandard"))
                                {
                                    this.logSave(" - HSStandard shader family detected for Accessories, trying to assign RenderType...");
                                    this.logSave("  (Rendering) Mode: " + material.GetFloat("_Mode"));
                                    bool isAlphaTest = material.IsKeywordEnabled("_ALPHATEST_ON");
                                    bool isAlphaPremultiply = material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");
                                    bool isAlphaBlend = material.IsKeywordEnabled("_ALPHABLEND_ON");

                                    this.logSave("  (TEST, PREMULTIPLY, BLEND) = " + isAlphaTest + "," + isAlphaPremultiply + "," + isAlphaBlend);

                                    if (isAlphaTest) material.SetOverrideTag("RenderType", "TransparentCutout");
                                    if (isAlphaPremultiply) material.SetOverrideTag("RenderType", "Transparent");
                                    if (isAlphaBlend) material.SetOverrideTag("RenderType", "Transparent");

                                    this.logSave("  RenderType: " + material.GetTag("RenderType", false));
                                }
                            }
                            else
                            {
                                //if (rq <= 2500)
                                //{
                                //    this.logSave("Unable to map shader " + HoneyPot.inspector[inspector_key] + " to PH presets we have. Default to " + HoneyPot.presets["PBRsp_3mask"].shader.name);
                                //    material.shader = HoneyPot.presets["PBRsp_3mask"].shader;
                                //}
                                //else
                                //{
                                material.shader = HoneyPot.presets["Standard"].shader;
                                this.logSave("Unable to map shader " + HoneyPot.inspector[inspector_key] + " to PH presets we have. Default to " + HoneyPot.presets["Standard"].shader.name/* + " with high RQ to get transparency."*/);
                                this.logSave("  (Rendering) Mode: " + material.GetFloat("_Mode"));
                                bool isAlphaTest = material.IsKeywordEnabled("_ALPHATEST_ON");
                                bool isAlphaPremultiply = material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");
                                bool isAlphaBlend = material.IsKeywordEnabled("_ALPHABLEND_ON");

                                this.logSave("  (TEST, PREMULTIPLY, BLEND) = " + isAlphaTest + "," + isAlphaPremultiply + "," + isAlphaBlend);

                                if (isAlphaTest) material.SetOverrideTag("RenderType", "TransparentCutout");
                                if (isAlphaPremultiply) material.SetOverrideTag("RenderType", "Transparent");
                                if (isAlphaBlend) material.SetOverrideTag("RenderType", "Transparent");

                                this.logSave("  RenderType: " + material.GetTag("RenderType", false));
                                //}
                            }
                        }
                        material.renderQueue = rq;
                        //important: RQ assignment has to go after shader assignment. 
                        //           fucking implicit setter changes stuff... 
                    }

                    if (material.renderQueue <= 3600)
                    {   //Note: This is because we don't want to include glasses like accessories!!
                        //Note: It seems all glasses (transparent Standard) materials are around RQ 3800 or more
                        if( !list.Contains(material_name) )
                            list.Add(material_name);
                        try_this_shader_name = material.shader.name;
                    }
                }
            }

            //Note: wait DUDE what the fuck. acceobj_obj is always going to be "AcceParent" which will never contain 
            //      a MaterialCustoms. I suppose I can still always assume that AcceParent has only 1 child?
            GameObject the_actual_acce_obj = acceobj_obj.transform.GetChild(0).gameObject;
            MaterialCustoms materialCustoms = the_actual_acce_obj.GetComponent<MaterialCustoms>();
            if (materialCustoms == null && try_this_shader_name != "")
            {
                this.logSave(" -- This accessory doesn't have MaterialCustoms, try adding one: " + accessoryData.assetbundleName.Replace("\\", "/") + ", shader: " + try_this_shader_name);
                materialCustoms = the_actual_acce_obj.AddComponent<MaterialCustoms>();
                materialCustoms.parameters = new MaterialCustoms.Parameter[HoneyPot.mc.parameters.Length];
                int idx = 0;
                foreach (MaterialCustoms.Parameter copy in HoneyPot.mc.parameters)
                {
                    materialCustoms.parameters[idx] = new MaterialCustoms.Parameter(copy);
                    materialCustoms.parameters[idx++].materialNames = list.ToArray();
                }
                match_correct_shader_property(materialCustoms, try_this_shader_name);
                MaterialCustoms_Setup.Invoke(materialCustoms, new object[0]);
                match_correct_shader_property_data_range(materialCustoms, try_this_shader_name);
            }
            acce.UpdateColorCustom(slot);
        }
        #endregion

        #region Wears shader remapping
        private static FieldInfo wearcustomedit_nowtabField = typeof(WearCustomEdit).GetField("nowTab", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo Wears_bodySkinMeshField    = typeof(Wears).GetField("bodySkinMesh", BindingFlags.Instance | BindingFlags.NonPublic); 
        private static MethodInfo WearObj_SetupMaterials    = typeof(WearObj).GetMethod("SetupMaterials", new Type[] { typeof(WearData) });

        [HarmonyPatch(typeof(Wears), "WearInstantiate")]
        [HarmonyPostfix]
        private static void Postfix(Wears __instance, WEAR_TYPE type, Material skinMaterial, Material customHighlightMat_Skin)
        {
            if (type == WEAR_TYPE.TOP)
            {
                if (__instance.GetWearObj(type) == null) return;

                GameObject obj = __instance.GetWearObj(type).obj;
                Renderer[] componentsInChildren = obj.GetComponentsInChildren<Renderer>(true);
                bool bodyskin_substituted = false;
                foreach (Renderer renderer in componentsInChildren)
                {   // Make sure these leftover default materials are also replaced by the naked body skin
                    if (renderer.sharedMaterial == null) continue;
                    if (renderer.sharedMaterial.name.Contains_NoCase("lambert") ||
                        renderer.sharedMaterial.name.Contains_NoCase("clipping"))
                    {
                        renderer.sharedMaterial = (Wears_bodySkinMeshField.GetValue(__instance) as SkinnedMeshRenderer).sharedMaterial;
                        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                        bodyskin_substituted = true;
                    }
                }
                if( bodyskin_substituted )
                    __instance.ChangeBodyMaterial(Wears_bodySkinMeshField.GetValue(__instance) as SkinnedMeshRenderer);
            }
            self.setWearShader(__instance, (int)type, type, (type == WEAR_TYPE.BRA || type == WEAR_TYPE.SHORTS) ? true : false);
        }

        public void setWearShader(Wears wears, int idx, WEAR_TYPE type, bool forceColorable = false)
        {
            WearObj wearobj = wears.GetWearObj(type);
            if (wearobj == null)
            {
                return;
            }
            try
            {
                WearData wearData = wears.GetWearData(type);

                GameObject wearobj_obj = wearobj.obj;
                Renderer[] renderers_in_wearobj = wearobj_obj.GetComponentsInChildren<Renderer>(true);
                string backup_shader_name = "";
                string priority_shader_name = "";
                List<string> list_objcolor = new List<string>();
                List<string> list_all_materials_without_body_material_mpoint = new List<string>();

                foreach (Renderer renderer in renderers_in_wearobj)
                {
                    foreach (Material material in renderer.materials)
                    {
                        // NOTE: Adding a note to myself here -- Apparently there are HS top clothes 
                        //       that added a temporary or almost-empty material to o_body_a and/or o_body_b.
                        //       When the intent isn't total subsitution of the human body (like in some male tops do)
                        //       It simply breaks PH's body/top loading process due to missing textures I presume. 
                        //       Because the main game procedures asks for them. 
                        //       This is extremely problematic because I cannot fix them in this function, 
                        //       and further patches to Body or Wears classes are needed. I am not sure how to do them yet
                        //       The only way to avoid mistakes like this right now is to use SB3U to remove those
                        //       materials manually. Which is easily-doable, but nonetheless an annoyance, 
                        //       and they are not exactly rare occurrances. 
                        string material_name = material.name.Replace(" (Instance)", "");
                        string inspector_key = wearData.assetbundleName.Replace("\\", "/") + "|" + material_name;
                        //Note @WTFsetWearShader: OK, so, this conditional is unnecessarily cluttered, 
                        //                        and I am going to guess a lot of the conditions are overlapping
                        //                        however there are just too many special cases. 
                        //                        I have to kinda assume every condition was there for a reason. 
                        //                        So I am not going to make this part any simpler.
                        bool avoid_body_material_mpoint_flag =
                            !material.name.Contains("cf_m_body_CustomMaterial") &&
                            !material.name.Contains("cm_m_body_CustomMaterial") &&
                            !material.name.Contains("cm_M_point") &&
                            !material.name.Contains("cf_M_point") && //What is this anyway????
                            !renderer.tag.Contains("New tag (8)"); //Apparently 4E28 == New tag (8) and 
                                                                   //we don't have ObjSkinBody tag in PH???
                        if ((type != WEAR_TYPE.TOP ||
                            avoid_body_material_mpoint_flag) &&
                            material.shader.name.IsNullOrEmpty()) //This is the only way we know it's from HS.
                        {
                            int rq = getRenderQueue(inspector_key, renderer.gameObject.GetComponent<SetRenderQueue>());
                            this.logSave("Wear material: " + inspector_key + " RQ: " + rq);
                            if (HoneyPot.inspector.ContainsKey(inspector_key))
                            {
                                if (HoneyPot.presets.ContainsKey(HoneyPot.inspector[inspector_key]))
                                {
                                    material.shader = HoneyPot.presets[HoneyPot.inspector[inspector_key]].shader;
                                    this.logSave("shader: " + HoneyPot.inspector[inspector_key] + " ==> " + material.shader.name);
                                    if (material.shader.name.Contains("HSStandard"))
                                    {
                                        this.logSave(" - HSStandard shader family detected for clothing, trying to assign RenderType...");
                                        this.logSave("  (Rendering) Mode: " + material.GetFloat("_Mode"));
                                        bool isAlphaTest = material.IsKeywordEnabled("_ALPHATEST_ON");
                                        bool isAlphaPremultiply = material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");
                                        bool isAlphaBlend = material.IsKeywordEnabled("_ALPHABLEND_ON");

                                        this.logSave("  (TEST, PREMULTIPLY, BLEND) = " + isAlphaTest + "," + isAlphaPremultiply + "," + isAlphaBlend);

                                        if (isAlphaTest) material.SetOverrideTag("RenderType", "TransparentCutout");
                                        if (isAlphaPremultiply) material.SetOverrideTag("RenderType", "Transparent");
                                        if (isAlphaBlend) material.SetOverrideTag("RenderType", "Transparent");

                                        this.logSave("  RenderType: " + material.GetTag("RenderType", false));
                                    }
                                }
                                else
                                {
                                    //if (rq <= 2500)
                                    //{
                                    //    this.logSave("Unable to map shader " + HoneyPot.inspector[inspector_key] + " to PH presets we have. Default to PBRsp_3mask as we find RQ value <= 2500.");
                                    //    material.shader = HoneyPot.presets["PBRsp_3mask"].shader;
                                    //}
                                    //else
                                    //{
                                    //    this.logSave("Unable to map shader " + HoneyPot.inspector[inspector_key] + " to PH presets we have. Default to PBRsp_3mask_alpha as we find RQ value > 2500.");
                                    //    material.shader = HoneyPot.presets["PBRsp_3mask_alpha"].shader;
                                    //}
                                    material.shader = HoneyPot.presets["Standard"].shader;
                                    this.logSave(" - Unable to map shader " + HoneyPot.inspector[inspector_key] + " to PH presets we have. Default to Standard.");
                                    this.logSave("  (Rendering) Mode: " + material.GetFloat("_Mode"));
                                    bool isAlphaTest = material.IsKeywordEnabled("_ALPHATEST_ON");
                                    bool isAlphaPremultiply = material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");
                                    bool isAlphaBlend = material.IsKeywordEnabled("_ALPHABLEND_ON");

                                    this.logSave("  (TEST, PREMULTIPLY, BLEND) = " + isAlphaTest + "," + isAlphaPremultiply + "," + isAlphaBlend);

                                    if (isAlphaTest) material.SetOverrideTag("RenderType", "TransparentCutout");
                                    if (isAlphaPremultiply) material.SetOverrideTag("RenderType", "Transparent");
                                    if (isAlphaBlend) material.SetOverrideTag("RenderType", "Transparent");

                                    this.logSave("  RenderType: " + material.GetTag("RenderType", false));
                                }
                            }
                            material.renderQueue = rq;
                        }

                        if (avoid_body_material_mpoint_flag)
                        {
                            if (!list_all_materials_without_body_material_mpoint.Contains(material_name))
                            {
                                list_all_materials_without_body_material_mpoint.Add(material_name);
                                //Note: No, we can't put the list_objcolor block in this because it might be
                                //      multiple renderers with different tags that uses the same material.
                            }
                            if (renderer.tag.Contains("ObjColor") && !list_objcolor.Contains(material_name))
                            {
                                list_objcolor.Add(material_name);
                                priority_shader_name = material.shader.name;
                                //Note: Since we know this renderer has ObjColor, it **must** intended to be colored.
                                //      so its material and shader must be the one that is more suitable for coloring.
                            }
                        }
                        backup_shader_name = material.shader.name; //take whatever useful material as a backup.
                    }
                }

                if ((list_objcolor.Count == 0 && force_color_everything_that_doesnt_have_materialcustoms)
                    || forceColorable)
                {   //Note: list_objcolor == 0 means we didn't find priority_shader_name either.
                    //Note: forceColorable would color every renderer unconditionally.
                    list_objcolor = list_all_materials_without_body_material_mpoint;
                    priority_shader_name = backup_shader_name;
                }

                MaterialCustoms materialCustoms = wearobj_obj.GetComponent<MaterialCustoms>();
                if (materialCustoms == null && list_objcolor.Count != 0)
                {
                    this.logSave(" -- This wear doesn't have MaterialCustoms, try adding one: " + wearData.assetbundleName.Replace("\\", "/") + ", shader: " + priority_shader_name);
                    materialCustoms = wearobj_obj.AddComponent<MaterialCustoms>();
                    materialCustoms.parameters = new MaterialCustoms.Parameter[HoneyPot.mc.parameters.Length];
                    int k = 0;
                    foreach (MaterialCustoms.Parameter copy in HoneyPot.mc.parameters)
                    {
                        materialCustoms.parameters[k] = new MaterialCustoms.Parameter(copy);
                        materialCustoms.parameters[k++].materialNames = list_objcolor.ToArray();
                    }
                    match_correct_shader_property(materialCustoms, priority_shader_name);
                    MaterialCustoms_Setup.Invoke(materialCustoms, new object[0]);
                }
                WearObj_SetupMaterials.Invoke(wearobj, new object[] { null }); //the WearData is never used/checked in this call
                wearobj.UpdateColorCustom();
                if (this.wearCustomEdit != null && (int)wearcustomedit_nowtabField.GetValue(this.wearCustomEdit) == idx)
                {
                    // After a HS clothing is loaded, if wearCustomEdit is present and it is choosing the this wear slot
                    // Try to force the LoadedCoordinate() to enable color UI. Because before this point in time
                    // This HS clothing is deemed non-colorchangable because of its MaterialCustom is not set.
                    this.wearCustomEdit.LoadedCoordinate(type);
                }
                //}
            }
            catch (Exception ex)
            {
                this.logSave(ex.ToString());
            }
        }
        #endregion

        #region Shader preparations
        private void readInspector()
        {
            StreamReader streamReader = new StreamReader(this.inspectorText, Encoding.UTF8);
            string text;
            while ((text = streamReader.ReadLine()) != null)
            {
                try
                {
                    string[] array = text.Split(new char[]
                    {
                        ','
                    });
                    if (array.Length != 0)
                    {
                        string key = array[0];
                        string value = "";
                        int value2 = -2;
                        if (array.Length > 1)
                        {
                            value = array[1];
                            if (array.Length > 2)
                            {
                                value2 = int.Parse(array[2]);
                            }
                        }
                        HoneyPot.inspector[key] = value;
                        HoneyPot.material_rq[key] = value2;
                    }
                }
                catch (Exception)
                {
                }
            }
            streamReader.Close();
        }

        public List<string> readPresetShaderString()
        {
            List<string> list = new List<string>();
            StreamReader streamReader = new StreamReader(this.shaderText);
            string item;
            while ((item = streamReader.ReadLine()) != null)
            {
                list.Add(item);
            }
            streamReader.Close();
            return list;
        }

        private void loadShaderMapping()
        {
            try
            {
                foreach (string text in this.readPresetShaderString())
                {
                    if (text.Length >= 2)
                    {
                        string[] array = text.Split(new char[]
                        {
                            '|'
                        });
                        if (array.Length == 2)
                        {
                            PresetShader presetShader = new PresetShader();
                            presetShader.shader = PH_shaders[array[1]];
                            HoneyPot.presets.Add(array[0], presetShader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.logSave(ex.ToString());
            }

            HoneyPot.orgShader = PH_shaders["Shader Forge/Hair/ShaderForge_Hair"];

            // Adding two specific PresetShader only for simple particle effects: 
            if (!HoneyPot.presets.ContainsKey("Particle Add"))
            {
                HoneyPot.presets.Add("Particle Add", new PresetShader());
                HoneyPot.presets["Particle Add"].shader = Shader.Find("Particles/Additive");
            }
            if (!HoneyPot.presets.ContainsKey("Particle Alpha Blend"))
            {
                HoneyPot.presets.Add("Particle Alpha Blend", new PresetShader());
                HoneyPot.presets["Particle Alpha Blend"].shader = Shader.Find("Particles/Alpha Blended");
            }
        }

        private AssetBundle loadEmbeddedAssetBundle(string embedded_name)
        {
            Stream shader_rawstream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embedded_name);
            byte[] buffer = new byte[16 * 1024];
            byte[] rawbyte;
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = shader_rawstream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                rawbyte = ms.ToArray();
                ms.Close();
            }
            shader_rawstream.Close();
            return AssetBundle.LoadFromMemory(rawbyte);
        }

        private void readAllPHShaders()
        {
            AssetBundle bundle = loadEmbeddedAssetBundle("ClassLibrary4.ph_shaders.unity3d");
            Shader[] all_shaders = bundle.LoadAllAssets<Shader>();
            foreach (Shader s in all_shaders)
            {
                this.logSave("Found " + s.name + " in the bundle.");
                HoneyPot.PH_shaders[s.name] = s;
            }
            Material[] all_materials = bundle.LoadAllAssets<Material>();
            foreach (Material m in all_materials)
            {
                if (!HoneyPot.PH_shaders.ContainsKey(m.shader.name))
                {
                    this.logSave("Found " + m.shader.name + " in the bundle additionally.");
                    HoneyPot.PH_shaders[m.shader.name] = m.shader;
                }
            }
            GameObject asset_standard = bundle.LoadAsset<GameObject>("asset_standard");
            Renderer r = asset_standard.GetComponentInChildren<Renderer>();
            if (r.materials[0].shader != null)
            {
                HoneyPot.PH_shaders["Standard"] = r.materials[0].shader;
            }
            else
            {
                this.logSave("Somehow Standard shader cannot be loaded.");
            }

            // Thank AgiShark for this!!!!!
            AssetBundle bundle_HSStandard_PH = loadEmbeddedAssetBundle("ClassLibrary4.hsstandardshaders");
            Material[] all_hsstandard_materials = bundle_HSStandard_PH.LoadAllAssets<Material>();
            foreach (Material m in all_hsstandard_materials)
            {
                if (!HoneyPot.PH_shaders.ContainsKey(m.shader.name))
                {
                    this.logSave("Found " + m.shader.name + " in hsstandardshaders.");
                    HoneyPot.PH_shaders[m.shader.name] = m.shader;
                }
            }
            this.logSave(HoneyPot.PH_shaders.Count + " shaders found.");

            GameObject proxy_to_get_material_customs = bundle.LoadAsset<GameObject>("p_cf_yayoi_top");
            HoneyPot.mc = proxy_to_get_material_customs.GetComponentInChildren<MaterialCustoms>();

            bundle.Unload(false);
            bundle_HSStandard_PH.Unload(false);
        }
        #endregion

        #region Processing CustomDataManager, lists & moving them, noticing conflicts. 
        private void addConflict(int id, string asset1, string asset2, string name1, string name2)
        {
            if (!asset1.Equals(asset2))
            {
                HoneyPot.conflictList.Add(string.Concat(
                    new object[] { "[conflict] id:", id, ",asset:", name1, "(", asset1, ") - ", name2, "(", asset2, ")" }
                ));
            }
        }

        public void exportConflict()
		{
			try
			{
                this.logSave("HoneyPot found " + HoneyPot.conflictList.Count + " ID conflicts in total. Please check UserData/conflict.txt");
				StreamWriter streamWriter = new FileInfo(this.conflictText).CreateText();
				foreach (string value in HoneyPot.conflictList)
				{
					streamWriter.WriteLine(value);
				}
				streamWriter.Flush();
				streamWriter.Close();
				HoneyPot.conflictList.Clear();
			}
            catch (Exception ex)
            {
                this.logSave(ex.ToString());
            }
        }

        //Note: adapted from XUnity.Common.Utilities
        public static void RandomizeCabWithAnyLength(byte[] assetBundleData)
        {
            FindAndReplaceCab(assetBundleData, 2048);
        }
        private static void FindAndReplaceCab(byte[] data, int maxIterations = -1)
        {
            var len = Math.Min(data.Length, maxIterations);
            if (len == -1) len = data.Length;

            char c;
            byte b;
            var newCab = "CAB-" + Guid.NewGuid().ToString("N");
            int cabIdx = 0;
            int startingIdx = 71;  //Some CAB actually start before 72.
            byte[] preCab = { 67, 65, 66 };
            int preCabIdx = 0;
            while( preCabIdx < 3 && startingIdx < 128 )
            {
                if (data[startingIdx] == preCab[preCabIdx]) preCabIdx++;
                else preCabIdx = 0;
                startingIdx++;
            }
            if (startingIdx >= 128) startingIdx = 72; //Meaning it doesn't even have "CAB" in the header
            //But is it possible for the CAB string without "CAB" to start before 72 ???
            else startingIdx = startingIdx - 3;

            int endingIdx = startingIdx;
            while (data[endingIdx++] >= 32) ; 
            //Treat any ascii lower than whitespace as a termination (just a guess)            
            endingIdx--;
            //self.logSave("newCab: " + newCab);
            //StringBuilder src = new StringBuilder();
            //StringBuilder dst = new StringBuilder();
            for (int i = startingIdx; i < endingIdx && cabIdx < newCab.Length; i++)
            { //Don't really care for CAB string longer than 32. We've substitute enough anyway.
                b = data[i];
                c = (char)b;
                //src.Append(newCab[cabIdx]);
                //dst.Append((char)data[i]);
                data[i] = (byte)newCab[cabIdx++];
            }
            //self.logSave(" - src used: " + src.ToString());
            //self.logSave(" - dst subd: " + dst.ToString());
        }
        //Note: End of adapted code from XUnity.Common.Utilities

        private int extract_hi_3digits(int id)
        {
            int div = 1;                      
            while (div <= id / 10) div *= 10;   //e.g. id = 1234567, div = 1000000
            div /= 100;                         //     div = 10000
            return id / div;                    //     return 1234567 / 10000 => 123
        }

        static int asynctracker = 0;

        public IEnumerator getListContent(string assetBundleDir, string fileName)
        {
            Dictionary<int, AccessoryData> acc_dict = null;
            Dictionary<int, HairData> f_hair_dict = null;
            Dictionary<int, BackHairData> f_hairB_dict = null;
            Dictionary<int, WearData> f_socks_dict = null;
            Dictionary<int, WearData> f_shoe_dict = null;
            Dictionary<int, WearData> f_swimbot_dict = null;
            Dictionary<int, WearData> f_swim_dict = null;
            Dictionary<int, WearData> f_swimtop_dict = null;
            Dictionary<int, WearData> f_bra_dict = null;
            Dictionary<int, WearData> f_shorts_dict = null;
            Dictionary<int, WearData> f_glove_dict = null;
            Dictionary<int, WearData> f_bot_dict = null;
            Dictionary<int, WearData> f_panst_dict = null;
            Dictionary<int, WearData> f_top_dict = null;
            Dictionary<int, PrefabData> f_brow_dict = null;
            Dictionary<int, PrefabData> eyelash_dict = null;
            Dictionary<int, WearData> m_wear_dict = null;
            Dictionary<int, WearData> m_shoe_dict = null;
            Dictionary<int, BackHairData> m_hair_dict = null;
            //try
            {   
                AssetBundleCreateRequest abcr = AssetBundle.LoadFromFileAsync(assetBundleDir + "/" + fileName);
                asynctracker++;
                //yield return abcr;
                while( !abcr.isDone )
                {
                    abcr.allowSceneActivation = true;
                    yield return null;
                }
                AssetBundle ab = abcr.assetBundle;
                if (ab == null)
                {         
                    this.logSave("Loading " + fileName + " probably failed due to CAB-string issue. Reloading & changing cab..." );
                    byte[] buffer = File.ReadAllBytes(assetBundleDir + "/" + fileName);
                    RandomizeCabWithAnyLength(buffer);
                    abcr = AssetBundle.LoadFromMemoryAsync(buffer);
                    while (!abcr.isDone)
                    {
                        abcr.allowSceneActivation = true;
                        yield return null;
                    }
                    ab = abcr.assetBundle;
                }
                foreach (TextAsset textAsset in ab.LoadAllAssets<TextAsset>())
                {
                    if (textAsset.name.Contains("ca_f_head"))
                    {
                        acc_dict = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.HEAD);
                    }
                    else if (textAsset.name.Contains("ca_f_hand"))
                    {
                        acc_dict = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.HAND);
                    }
                    else if (textAsset.name.Contains("ca_f_arm"))
                    {
                        acc_dict = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.ARM);
                    }
                    else if (textAsset.name.Contains("ca_f_back"))
                    {
                        acc_dict = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.BACK);
                    }
                    else if (textAsset.name.Contains("ca_f_breast"))
                    {
                        acc_dict = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.CHEST);
                    }
                    else if (textAsset.name.Contains("ca_f_ear"))
                    {
                        acc_dict = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.EAR);
                    }
                    else if (textAsset.name.Contains("ca_f_face"))
                    {
                        acc_dict = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.FACE);
                    }
                    else if (textAsset.name.Contains("ca_f_leg"))
                    {
                        acc_dict = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.LEG);
                    }
                    else if (textAsset.name.Contains("ca_f_megane"))
                    {
                        acc_dict = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.GLASSES);
                    }
                    else if (textAsset.name.Contains("ca_f_neck"))
                    {
                        acc_dict = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.NECK);
                    }
                    else if (textAsset.name.Contains("ca_f_shoulder"))
                    {
                        acc_dict = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.SHOULDER);
                    }
                    else if (textAsset.name.Contains("ca_f_waist"))
                    {
                        acc_dict = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.WAIST);
                    }
                    else if (textAsset.name.Contains("cf_f_hairB"))
                    {
                        f_hairB_dict = CustomDataManager.Hair_b;
                    }
                    else if (textAsset.name.Contains("cf_f_hairF"))
                    {
                        f_hair_dict = CustomDataManager.Hair_f;
                    }
                    else if (textAsset.name.Contains("cf_f_hairS"))
                    {   
                        //Note: Ok this is a bit strange. Of course we could use the same dictionary if the 
                        //      signature type is the same. But doesn't this mean we also mix up the ID sections?
                        f_hair_dict = CustomDataManager.Hair_s;
                    }
                    else if (textAsset.name.Contains("cm_f_hair"))
                    {
                        m_hair_dict = CustomDataManager.Hair_Male;
                    }
                    else if (textAsset.name.Contains("cf_f_socks"))
                    {
                        f_socks_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SOCKS);
                    }
                    else if (textAsset.name.Contains("cf_f_shoes"))
                    {
                        f_shoe_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SHOES);
                    }
                    else if (textAsset.name.Contains("cf_f_swimbot"))
                    {
                        f_swimbot_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SWIM_BOTTOM);
                    }
                    else if (textAsset.name.Contains("cf_f_swimtop"))
                    {
                        f_swimtop_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SWIM_TOP);
                    }
                    else if (textAsset.name.Contains("cf_f_swim"))
                    {
                        f_swim_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SWIM);
                    }
                    else if (textAsset.name.Contains("cf_f_bra"))
                    {
                        f_bra_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.BRA);
                    }
                    else if (textAsset.name.Contains("cf_f_shorts"))
                    {
                        f_shorts_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SHORTS);
                    }
                    else if (textAsset.name.Contains("cf_f_glove"))
                    {
                        f_glove_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.GLOVE);
                    }
                    else if (textAsset.name.Contains("cf_f_panst"))
                    {
                        f_panst_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.PANST);
                    }
                    else if (textAsset.name.Contains("cf_f_bot"))
                    {
                        f_bot_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.BOTTOM);
                    }
                    else if (textAsset.name.Contains("cf_f_top"))
                    {
                        f_top_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.TOP);
                    }
                    else if (textAsset.name.Contains("cf_m_eyebrow"))
                    {
                        f_brow_dict = CustomDataManager.Eyebrow_Female;
                    }
                    else if (textAsset.name.Contains("cf_m_eyelashes"))
                    {
                        eyelash_dict = CustomDataManager.Eyelash;
                    }
                    else if (textAsset.name.Contains("cm_f_body"))
                    {
                        m_wear_dict = CustomDataManager.GetWearDictionary_Male(WEAR_TYPE.TOP);
                    }
                    else if (textAsset.name.Contains("cm_f_shoes"))
                    {
                        m_shoe_dict = CustomDataManager.GetWearDictionary_Male(WEAR_TYPE.SHOES);
                    }
                    if (m_shoe_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int j = 0; j < all_lines.Length; j++)
                        {
                            string[] cells = all_lines[j].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {                        
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                    //Trace.Assert( int.Parse(cells[0].Substring(0, 3)) == extract_hi_3digits(og_id) );
                                }
                                else
                                {
                                    num += 838000;
                                }

                                if( !File.Exists(assetBundleDir + "/" + cells[4]) )
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                WearData wearData = new WearData(num, cells[2], cells[4], cells[5], m_shoe_dict.Count, false);
                                wearData.id = num;
                                if (!m_shoe_dict.ContainsKey(wearData.id))
                                {
                                    m_shoe_dict.Add(wearData.id, wearData);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, m_shoe_dict[num].assetbundleName + "/" + m_shoe_dict[num].prefab, wearData.assetbundleName + "/" + wearData.prefab, m_shoe_dict[num].name, wearData.name);
                                }
                            }
                        }
                    }
                    if (m_wear_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int k = 0; k < all_lines.Length; k++)
                        {
                            string[] cells = all_lines[k].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 837000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                WearData wearData2 = new WearData(num, cells[2], cells[4], cells[5], m_wear_dict.Count, false);
                                wearData2.id = num;
                                if (!m_wear_dict.ContainsKey(wearData2.id))
                                {
                                    m_wear_dict.Add(wearData2.id, wearData2);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, m_wear_dict[num].assetbundleName + "/" + m_wear_dict[num].prefab, wearData2.assetbundleName + "/" + wearData2.prefab, m_wear_dict[num].name, wearData2.name);
                                }
                            }
                        }
                    }
                    if (f_brow_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 835000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                PrefabData prefabData = new PrefabData(num, cells[2], cells[4], cells[5], f_brow_dict.Count, false);
                                prefabData.id = num;
                                if (!f_brow_dict.ContainsKey(prefabData.id))
                                {
                                    f_brow_dict.Add(prefabData.id, prefabData);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, f_brow_dict[num].assetbundleName + "/" + f_brow_dict[num].prefab, prefabData.assetbundleName + "/" + prefabData.prefab, f_brow_dict[num].name, prefabData.name);
                                }
                            }
                        }
                    }
                    if (eyelash_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 836000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                PrefabData prefabData2 = new PrefabData(num, cells[2], cells[4], cells[5], eyelash_dict.Count, false);
                                prefabData2.id = num;
                                if (!eyelash_dict.ContainsKey(prefabData2.id))
                                {
                                    eyelash_dict.Add(prefabData2.id, prefabData2);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, eyelash_dict[num].assetbundleName + "/" + eyelash_dict[num].prefab, prefabData2.assetbundleName + "/" + prefabData2.prefab, eyelash_dict[num].name, prefabData2.name);
                                }
                            }
                        }
                    }
                    if (f_bot_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 821000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                WearData wearData = new WearData(num, cells[2], cells[4], cells[6], f_bot_dict.Count, false);
                                wearData.id = num;
                                if (!f_bot_dict.ContainsKey(wearData.id))
                                {
                                    wearData.liquid = cells[11];
                                    wearData.coordinates = int.Parse(cells[14]);
                                    wearData.shortsDisable = (!cells[16].Equals("0"));

                                    f_bot_dict.Add(wearData.id, wearData);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, f_bot_dict[num].assetbundleName + "/" + f_bot_dict[num].prefab, wearData.assetbundleName + "/" + wearData.prefab, f_bot_dict[num].name, wearData.name);
                                }
                            }
                        }
                    }
                    if (f_top_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 820000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                WearData wearData = new WearData(num, cells[2], cells[4], cells[6], f_top_dict.Count, false);
                                wearData.id = num;
                                if (!f_top_dict.ContainsKey(wearData.id))
                                {
                                    wearData.liquid = cells[11];
                                    wearData.coordinates = int.Parse(cells[14]);
                                    //wearData.shortsDisable = (cells[14].Equals("2"));
                                    wearData.braDisable = (!cells[15].Equals("0"));
                                    wearData.nip = (!cells[17].Equals("0"));

                                    f_top_dict.Add(wearData.id, wearData);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, f_top_dict[num].assetbundleName + "/" + f_top_dict[num].prefab, wearData.assetbundleName + "/" + wearData.prefab, f_top_dict[num].name, wearData.name);
                                }
                            }
                        }
                    }
                    if (f_panst_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 828000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                WearData wearData5 = new WearData(num, cells[2], cells[4], cells[6], f_panst_dict.Count, false);
                                wearData5.id = num;
                                if (!f_panst_dict.ContainsKey(wearData5.id))
                                {
                                    f_panst_dict.Add(wearData5.id, wearData5);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, f_panst_dict[num].assetbundleName + "/" + f_panst_dict[num].prefab, wearData5.assetbundleName + "/" + wearData5.prefab, f_panst_dict[num].name, wearData5.name);
                                }
                            }
                        }
                    }
                    if (f_glove_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 827000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                WearData wearData6 = new WearData(num, cells[2], cells[4], cells[6], f_glove_dict.Count, false);
                                wearData6.id = num;
                                if (!f_glove_dict.ContainsKey(wearData6.id))
                                {
                                    f_glove_dict.Add(wearData6.id, wearData6);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, f_glove_dict[num].assetbundleName + "/" + f_glove_dict[num].prefab, wearData6.assetbundleName + "/" + wearData6.prefab, f_glove_dict[num].name, wearData6.name);
                                }
                            }
                        }
                    }
                    if (f_shorts_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 823000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                WearData wearData7 = new WearData(num, cells[2], cells[4], cells[6], f_shorts_dict.Count, false);
                                wearData7.id = num;
                                if (!f_shorts_dict.ContainsKey(wearData7.id))
                                {
                                    wearData7.liquid = cells[11];
                                    f_shorts_dict.Add(wearData7.id, wearData7);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, f_shorts_dict[num].assetbundleName + "/" + f_shorts_dict[num].prefab, wearData7.assetbundleName + "/" + wearData7.prefab, f_shorts_dict[num].name, wearData7.name);
                                }
                            }
                        }
                    }
                    if (f_bra_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 822000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                WearData wearData = new WearData(num, cells[2], cells[4], cells[6], f_bra_dict.Count, false);
                                wearData.id  = num;
                                wearData.nip = false; // NOTE: Curious. PH now activates nipple when Bra is shown regardless of this setting?
                                                      //       No nipple actually shows through though. So all fine?
                                WearData wearData9 = f_bra_dict[1];
                                if (!f_bra_dict.ContainsKey(wearData.id))
                                {
                                    wearData.liquid = cells[11];

                                    f_bra_dict.Add(wearData.id, wearData);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, f_bra_dict[num].assetbundleName + "/" + f_bra_dict[num].prefab, wearData.assetbundleName + "/" + wearData.prefab, f_bra_dict[num].name, wearData.name);
                                }
                            }
                        }
                    }
                    if (f_swimtop_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 825000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                WearData wearData10 = new WearData(num, cells[2], cells[4], cells[6], f_swimtop_dict.Count, false);
                                wearData10.id = num;
                                if (!f_swimtop_dict.ContainsKey(wearData10.id))
                                {
                                    wearData10.liquid = cells[11];
                                    f_swimtop_dict.Add(wearData10.id, wearData10);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, f_swimtop_dict[num].assetbundleName + "/" + f_swimtop_dict[num].prefab, wearData10.assetbundleName + "/" + wearData10.prefab, f_swimtop_dict[num].name, wearData10.name);
                                }
                            }
                        }
                    }
                    if (f_swim_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 824000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                WearData wearData = new WearData(num, cells[2], cells[4], cells[6], f_swim_dict.Count, false);
                                wearData.id = num;
                                if (!f_swim_dict.ContainsKey(wearData.id))
                                {
                                    // TODO: How do we specify that this swimsuit is top-bottom separated or not?
                                    wearData.liquid = cells[11];
                                    f_swim_dict.Add(wearData.id, wearData);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, f_swim_dict[num].assetbundleName + "/" + f_swim_dict[num].prefab, wearData.assetbundleName + "/" + wearData.prefab, f_swim_dict[num].name, wearData.name);
                                }
                            }
                        }
                    }
                    if (f_swimbot_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 826000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                WearData wearData12 = new WearData(num, cells[2], cells[4], cells[6], f_swimbot_dict.Count, false);
                                wearData12.id = num;
                                if (!f_swimbot_dict.ContainsKey(wearData12.id))
                                {
                                    wearData12.liquid = cells[11];
                                    f_swimbot_dict.Add(wearData12.id, wearData12);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, f_swimbot_dict[num].assetbundleName + "/" + f_swimbot_dict[num].prefab, wearData12.assetbundleName + "/" + wearData12.prefab, f_swimbot_dict[num].name, wearData12.name);
                                }
                            }
                        }
                    }
                    if (f_shoe_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 830000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                WearData wearData13 = new WearData(num, cells[2], cells[4], cells[6], f_shoe_dict.Count, false);
                                wearData13.id = num;
                                if (!f_shoe_dict.ContainsKey(wearData13.id))
                                {
                                    f_shoe_dict.Add(wearData13.id, wearData13);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, f_shoe_dict[num].assetbundleName + "/" + f_shoe_dict[num].prefab, wearData13.assetbundleName + "/" + wearData13.prefab, f_shoe_dict[num].name, wearData13.name);
                                }
                            }
                        }
                    }
                    if (f_socks_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 829000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                WearData wearData14 = new WearData(num, cells[2], cells[4], cells[6], f_socks_dict.Count, false);
                                wearData14.id = num;
                                if (!f_socks_dict.ContainsKey(wearData14.id))
                                {
                                    f_socks_dict.Add(wearData14.id, wearData14);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, f_socks_dict[num].assetbundleName + "/" + f_socks_dict[num].prefab, wearData14.assetbundleName + "/" + wearData14.prefab, f_socks_dict[num].name, wearData14.name);
                                }
                            }
                        }
                    }
                    if (acc_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 832000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                AccessoryData accessoryData = new AccessoryData(num, cells[2], cells[4], cells[5], cells[6], cells[8], ItemDataBase.SPECIAL.NONE, acc_dict.Count, false);
                                accessoryData.id = num;
                                if (!acc_dict.ContainsKey(accessoryData.id))
                                {
                                    acc_dict.Add(accessoryData.id, accessoryData);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, acc_dict[num].assetbundleName + "/" + acc_dict[num].prefab_F, accessoryData.assetbundleName + "/" + accessoryData.prefab_F, acc_dict[num].name, accessoryData.name);
                                }
                            }
                        }
                    }
                    if (f_hair_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {   // Note: So this section contains both F_HairF and F_HairS ?
                                    num += 833000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                HairData hairData = new HairData(num, cells[2], cells[4], cells[6], f_hair_dict.Count, false);
                                hairData.id = num;
                                if (!f_hair_dict.ContainsKey(hairData.id))
                                {
                                    f_hair_dict.Add(hairData.id, hairData);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, f_hair_dict[num].assetbundleName + "/" + f_hair_dict[num].prefab, hairData.assetbundleName + "/" + hairData.prefab, f_hair_dict[num].name, hairData.name);
                                }
                            }
                        }
                    }
                    if (f_hairB_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 834000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                BackHairData backHairData = new BackHairData(num, cells[2], cells[4], cells[6], f_hairB_dict.Count, false, "セミロング", "1".Equals(cells[13]));
                                backHairData.id = num;
                                if (!f_hairB_dict.ContainsKey(backHairData.id))
                                {
                                    f_hairB_dict.Add(backHairData.id, backHairData);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, f_hairB_dict[num].assetbundleName + "/" + f_hairB_dict[num].prefab, backHairData.assetbundleName + "/" + backHairData.prefab, f_hairB_dict[num].name, backHairData.name);
                                }
                            }
                        }
                    }
                    if (m_hair_dict != null)
                    {
                        string[] all_lines = textAsset.text.Replace("\r\n", "\n").Split(new char[]{'\n'});
                        for (int i = 0; i < all_lines.Length; i++)
                        {
                            string[] cells = all_lines[i].Split(new char[]{'\t'});
                            if (cells.Length > 3)
                            {
                                int og_id = int.Parse(cells[0]);
                                int num = og_id % 1000;
                                if (cells[0].Length > 6)
                                {
                                    num = og_id % 1000000 + extract_hi_3digits(og_id) * 1000;
                                }
                                else
                                {
                                    num += 839000;
                                }

                                if (!File.Exists(assetBundleDir + "/" + cells[4]))
                                {
                                    logSave("HoneyPot can't find file: " + cells[4] + ", from list: " + fileName);
                                    continue;
                                }

                                BackHairData backHairData = new BackHairData(num, cells[2], cells[4], cells[5], m_hair_dict.Count, false, "セミロング", true);
                                backHairData.id = num;
                                if (!m_hair_dict.ContainsKey(backHairData.id))
                                {
                                    m_hair_dict.Add(backHairData.id, backHairData);
                                    HoneyPot.idFileDict[num] = cells[4];
                                }
                                else
                                {
                                    this.addConflict(num, m_hair_dict[num].assetbundleName + "/" + m_hair_dict[num].prefab, backHairData.assetbundleName + "/" + backHairData.prefab, m_hair_dict[num].name, backHairData.name);
                                }
                            }
                        }
                    }
                    acc_dict = null;
                    f_hair_dict = null;
                    f_hairB_dict = null;
                    f_socks_dict = null;
                    f_shoe_dict = null;
                    f_swimbot_dict = null;
                    f_swim_dict = null;
                    f_swimbot_dict = null;
                    f_bra_dict = null;
                    f_shorts_dict = null;
                    f_glove_dict = null;
                    f_bot_dict = null;
                    f_panst_dict = null;
                    f_top_dict = null;
                    f_brow_dict = null;
                    eyelash_dict = null;
                    m_wear_dict = null;
                    m_shoe_dict = null;
                    m_hair_dict = null;
                }
                ab.Unload(true);
                asynctracker--;
            }

            //catch (Exception ex19)
            //{
            //    this.logSave("Error when reading: " + assetBundleDir + "/" + fileName);
            //    this.logSave(ex19.ToString());
            //}
        }

        private void transportDict(Dictionary<int, WearData> fromDict, Dictionary<int, WearData> toDict, int add, int order)
        {
            foreach (KeyValuePair<int, WearData> keyValuePair in fromDict)
            {
                WearData value = keyValuePair.Value;
                WearData wearData = new WearData(value.id, value.name, value.assetbundleName, value.prefab, order, false);
                wearData.id = keyValuePair.Key % 1000 + add;
                if (!toDict.ContainsKey(wearData.id))
                {
                    if (add == 828100)
                    {
                        wearData.name = "#" + wearData.name;
                    }
                    toDict.Add(wearData.id, wearData);
                    this.logSave("[wear add]" + wearData.name);
                }
            }
        }

        private void transportDicts()
        {
            string @string = ModPrefs.GetString("HoneyPot", "DoTransport", "", false);
            if ("FALSE".Equals(@string))
            {
                return;
            }
            try
            {
                this.logSave("HoneyPot is duplicating swimsuits into various non-swimsuits categories...");
                Dictionary<int, WearData> wearDictionary_Female = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.BRA);
                Dictionary<int, WearData> wearDictionary_Female2 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SHORTS);
                Dictionary<int, WearData> wearDictionary_Female3 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SWIM_BOTTOM);
                Dictionary<int, WearData> wearDictionary_Female4 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SWIM_TOP);
                Dictionary<int, WearData> wearDictionary_Female5 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.GLOVE);
                Dictionary<int, WearData> wearDictionary_Female6 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.PANST);
                Dictionary<int, WearData> wearDictionary_Female7 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.BOTTOM);
                Dictionary<int, WearData> wearDictionary_Female8 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SWIM);
                this.transportDict(wearDictionary_Female8, wearDictionary_Female, 829100, 95);
                this.transportDict(wearDictionary_Female8, wearDictionary_Female2, 828100, 94);
                this.transportDict(wearDictionary_Female4, wearDictionary_Female5, 825100, 91);
                this.transportDict(wearDictionary_Female3, wearDictionary_Female6, 826100, 92);
                this.transportDict(wearDictionary_Female7, wearDictionary_Female6, 827100, 93);
            }
            catch (Exception ex)
            {
                this.logSave(ex.ToString());
            }
        }
        #endregion

        #region Studio specific lists processing, manipulating category
        public void createCategory()
        {
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(30))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(30, "[MOD]家具");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(31))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(31, "[MOD]壁・板");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(32))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(32, "[MOD]日用品");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(33))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(33, "[MOD]小物");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(34))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(34, "[MOD]食材");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(35))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(35, "[MOD]武器");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(36))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(36, "[MOD]その他");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(37))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(37, "[MOD]Hアイテム");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(38))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(38, "[MOD]液体");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(39))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(39, "[MOD]画面効果");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(40))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(40, "[MOD]医療");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(41))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(41, "[MOD]エフェクト");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(50))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(50, "[MOD]基本形");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(53))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(53, "[MOD]オブジェ");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(57))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(57, "[MOD]キャラ");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(62))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(62, "[MOD]ギミック");
            }
            if (!Singleton<Info>.Instance.dicItemGroup.ContainsKey(63))
            {
                Singleton<Info>.Instance.dicItemGroup.Add(63, "[MOD]3DSE");
            }
        }

        private void readItemResolverText()
        {
            try
            {
                foreach (FileInfo fileInfo in new DirectoryInfo(assetBundlePath + "/studioneo/HoneyselectItemResolver").GetFiles())
                {
                    this.readItemResolverText(assetBundlePath, "studioneo/HoneyselectItemResolver/" + fileInfo.Name);
                }
            }
            catch (Exception ex)
            {
                this.logSave(ex.ToString());
            }
        }

        private void readItemResolverTextOld()
        {
            try
            {
                foreach (FileInfo fileInfo in new DirectoryInfo(assetBundlePath + "/studio/itemobj/honey/HoneyselectItemResolver").GetFiles())
                {
                    this.readItemResolverTextOld(assetBundlePath, "studio/itemobj/honey/HoneyselectItemResolver/" + fileInfo.Name);
                }
            }
            catch (Exception ex)
            {
                this.logSave(ex.ToString());
            }
        }

        public int toNewCategory(int cat)
        {
            switch (cat)
            {
                case 0:
                    return 50;
                case 1:
                    return 31;
                case 2:
                    return 30;
                case 3:
                    return 53;
                case 4:
                    return 34;
                case 5:
                    return 35;
                case 6:
                    return 33;
                case 7:
                    return 57;
                case 8:
                    return 37;
                case 9:
                    return 38;
                case 10:
                    return 39;
                case 11:
                    return 41;
                case 12:
                    return 62;
                case 13:
                    return 63;
                default:
                    switch (cat)
                    {
                        case 71:
                            return 71;
                        case 72:
                            return 72;
                        case 73:
                            return 73;
                        default:
                            if (cat != 99)
                            {
                                return cat;
                            }
                            return 99;
                    }
            }
        }

        public int oldCategoryToNewCategoryAdvance(int old)
        {
            switch (old)
            {
                case 0:
                    return 30;
                case 1:
                    return 31;
                case 2:
                    return 32;
                case 3:
                    return 33;
                case 4:
                    return 34;
                case 5:
                    return 35;
                case 6:
                    return 36;
                case 7:
                    return 37;
                case 8:
                    return 38;
                case 9:
                    return 39;
                case 10:
                    return 40;
                case 11:
                    return 41;
                default:
                    return 99;
            }
        }

        public void readItemResolverText(string assetBundleDir, string fileName)
        {
            StreamReader streamReader = new StreamReader(assetBundleDir + "/" + fileName, Encoding.UTF8);
            string text;
            while ((text = streamReader.ReadLine()) != null)
            {
                try
                {
                    if (text.IndexOf("#") != 0)
                    {
                        if (text.Length >= 2)
                        {
                            string[] array = text.Substring(1).Replace(">", "").Split(new char[]
                            {
                                '<'
                            });
                            if (array.Length >= 7)
                            {
                                Info.ItemLoadInfo itemLoadInfo = new Info.ItemLoadInfo();
                                itemLoadInfo.no = int.Parse(array[0]);
                                itemLoadInfo.group = this.toNewCategory(int.Parse(array[1]));
                                itemLoadInfo.name = array[2];
                                itemLoadInfo.manifest = ""; //array[3]; manifest doesn't seem to be really utilized, and we dump everything into abdata anyway. 
                                itemLoadInfo.bundlePath = array[4];
                                itemLoadInfo.fileName = array[5];
                                itemLoadInfo.childRoot = array[6];
                                itemLoadInfo.isAnime = array[7].ToLower().Equals("true");
                                itemLoadInfo.isColor = array[8].ToLower().Equals("true");
                                itemLoadInfo.colorTarget = array[9].Split(new char[]
                                {
                                    '/'
                                });
                                itemLoadInfo.isColor2 = array[10].ToLower().Equals("true");
                                itemLoadInfo.color2Target = array[11].Split(new char[]
                                {
                                    '/'
                                });
                                itemLoadInfo.isScale = array[12].ToLower().Equals("true");
                                if (!Singleton<Info>.Instance.dicItemLoadInfo.ContainsKey(itemLoadInfo.no))
                                {
                                    Singleton<Info>.Instance.dicItemLoadInfo.Add(itemLoadInfo.no, itemLoadInfo);
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            streamReader.Close();
        }

        public void readItemResolverTextOld(string assetBundleDir, string fileName)
        {
            StreamReader streamReader = new StreamReader(assetBundleDir + "/" + fileName, Encoding.UTF8);
            string text;
            while ((text = streamReader.ReadLine()) != null)
            {
                try
                {
                    if (text.IndexOf("#") != 0)
                    {
                        if (text.Length >= 2)
                        {
                            string[] array = text.Substring(1).Replace(">", "").Split(new char[]
                            {
                                '<'
                            });
                            if (array.Length >= 7)
                            {
                                Info.ItemLoadInfo itemLoadInfo = new Info.ItemLoadInfo();
                                itemLoadInfo.no = int.Parse(array[2]);
                                itemLoadInfo.group = this.oldCategoryToNewCategoryAdvance(int.Parse(array[3]));
                                itemLoadInfo.name = array[4];
                                itemLoadInfo.manifest = "";
                                itemLoadInfo.bundlePath = array[5];
                                itemLoadInfo.fileName = array[6];
                                itemLoadInfo.childRoot = "";
                                itemLoadInfo.isAnime = false;
                                itemLoadInfo.isColor = false;
                                itemLoadInfo.isColor2 = false;
                                itemLoadInfo.isScale = true;
                                if (!Singleton<Info>.Instance.dicItemLoadInfo.ContainsKey(itemLoadInfo.no))
                                {
                                    Singleton<Info>.Instance.dicItemLoadInfo.Add(itemLoadInfo.no, itemLoadInfo);
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            streamReader.Close();
        }
        #endregion

        private void Update()
		{
			if (HoneyPot.isFirst)
			{
                Stopwatch t = new Stopwatch();
                this.logSave("HoneyPot Debug: timer frequency: " + Stopwatch.Frequency);
                t.Start();
                this.readAllPHShaders();
                this.loadShaderMapping();
                this.logSave("Shader loaded timestamp: " + t.ElapsedMilliseconds.ToString());
				try
				{
                    FileInfo[] list = new DirectoryInfo(assetBundlePath + "/list/characustom").GetFiles("*.unity3d");
                    this.logSave("List GetFiles timestamp: " + t.ElapsedMilliseconds.ToString());
                    foreach (FileInfo fileInfo in list)
					{
						StartCoroutine(getListContent(assetBundlePath, "list/characustom/" + fileInfo.Name));
					}
				}
				catch (Exception ex)
				{
					this.logSave(ex.ToString());
				}
                this.logSave("List crawled timestamp: " + t.ElapsedMilliseconds.ToString());
				this.readInspector();
                this.logSave("Inspector established timestamp: " + t.ElapsedMilliseconds.ToString());
                HarmonyMethod acceobj_setupmaterials_transpiler = new HarmonyMethod(typeof(HoneyPot), nameof(AcceObj_SetupMaterials_Transpiler), new[] { typeof(IEnumerable<CodeInstruction>) });
                HarmonyMethod acceobj_updatecolorcustom_prefix  = new HarmonyMethod(typeof(HoneyPot), nameof(AcceObj_UpdateColorCustom_Prefix));
                HarmonyMethod head_changeeyebrow_postfix = new HarmonyMethod(typeof(HoneyPot), nameof(Head_ChangeEyebrow_Postfix));
                HarmonyMethod head_changeeyelash_postfix = new HarmonyMethod(typeof(HoneyPot), nameof(Head_ChangeEyelash_Postfix));
                harmony.Patch(typeof(Accessories).GetNestedType("AcceObj", BindingFlags.NonPublic).GetMethod("SetupMaterials", new Type[] { typeof(AccessoryData) }), transpiler: acceobj_setupmaterials_transpiler);
                harmony.Patch(typeof(Accessories).GetNestedType("AcceObj", BindingFlags.NonPublic).GetMethod("UpdateColorCustom"), prefix: acceobj_updatecolorcustom_prefix);
                harmony.Patch(typeof(Head).GetMethod("ChangeEyebrow"), postfix: head_changeeyebrow_postfix);
                harmony.Patch(typeof(Head).GetMethod("ChangeEyelash"), postfix: head_changeeyelash_postfix);
                this.logSave("Dynamic Harmony patches timestamp: " + t.ElapsedMilliseconds.ToString());
                if ( Singleton<Studio.Studio>.Instance != null )
                {
                    HarmonyMethod addobjectitem_load_postfix = new HarmonyMethod(typeof(HoneyPot), nameof(AddObjectItem_Load_Postfix));
                    harmony.Patch(typeof(AddObjectItem).GetMethod("Load", new Type[] { typeof(OIItemInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject) ,typeof(bool), typeof(int) }), postfix: addobjectitem_load_postfix );
                    this.readItemResolverText();
                    this.readItemResolverTextOld();
                    this.createCategory();
                    this.logSave("Studio patching and preparation timestamp: " + t.ElapsedMilliseconds.ToString());
                }
                HoneyPot.isFirst = false;
                t.Stop();
                this.logSave("All shader prepared - HoneyPot first run used time: " + t.ElapsedMilliseconds.ToString());
            }
            if( this.wearCustomEdit == null && SceneManager.GetActiveScene().name == "EditScene" ) 
            {
                this.wearCustomEdit = Resources.FindObjectsOfTypeAll<WearCustomEdit>()[0];
			}
            if( !HoneyPot.allGetListContentDone && HoneyPot.asynctracker == 0 )
            {
                this.logSave("HoneyPot all StartCoroutine(getListContent) are finished.");
                HoneyPot.allGetListContentDone = true;
                this.exportConflict();
                this.transportDicts();
            }
		}

        #region NotReallyRelated_To_HoneyPot: Mannequin BoneWeight fix if you changed the resources.assets FemaleBody
        //TODO: To_be_moved: This should be moved out of HoneyPot when I have time. 
        //      Should really be combined with the body mesh NML fixes that I hardcoded in Assembly-CSharp.dll
        private static Human reference_to_human = null;
        
        [HarmonyPatch(typeof(CoordinateCapture), "SetHuman")]
        [HarmonyPrefix]
        private static void Prefix(Human human)
        {
            reference_to_human = human;
        }

        [HarmonyPatch(typeof(SyncBoneWeight), "Awake")]
        [HarmonyPrefix]
        private static void Prefix(SyncBoneWeight __instance)
        {
            if (reference_to_human == null) return;

            Transform what_object_is_this = __instance.attachMeshRoot.transform;
            while (what_object_is_this.parent != null)
                what_object_is_this = what_object_is_this.parent;

            if (what_object_is_this.name != "EditMode") return;

            SkinnedMeshRenderer[] renderers = __instance.attachMeshRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Transform[] reference_bones = (Wears_bodySkinMeshField.GetValue(reference_to_human.wears) as SkinnedMeshRenderer).bones;
            Transform[] reordered_bones = new Transform[reference_bones.Length];

            foreach (SkinnedMeshRenderer r in renderers)
            {
                if (r.bones.Length < 1 || r.name != "cf_O_body_00") continue;
                if (r.bones.Length != reference_bones.Length)
                {
                    self.logSave("HoneyPot warning: SyncBoneWeight instance's cf_O_body_00 has different number of bones than our reference body.");
                }
                for (int i = 0; i < r.bones.Length && i < reference_bones.Length; i++)
                {
                    int j = 0;
                    while (reference_bones[i].name != r.bones[j].name) j++;
                    reordered_bones[i] = r.bones[j];
                }
                r.bones = reordered_bones;
            }
        }
        #endregion

        public void logSave(string txt)
        {
            Console.WriteLine(txt);
        }

        public void SetHarmony(Harmony input)
        {
            harmony = input;
        }

        private Harmony harmony;

        private static bool isFirst = true;
        private static bool allGetListContentDone = false;
        private bool force_color_everything_that_doesnt_have_materialcustoms = false;

        protected static Shader orgShader;
		protected static MaterialCustoms mc;

        private WearCustomEdit wearCustomEdit = null;

		private string assetBundlePath = Application.dataPath + "/../abdata";
		private string conflictText    = Application.dataPath + "/../UserData/conflict.txt";
		private string inspectorText   = Application.dataPath + "/../HoneyPot/HoneyPotInspector.txt";
		private string shaderText      = Application.dataPath + "/../HoneyPot/shader.txt";

		private static Dictionary<string, string> inspector     = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
		private static Dictionary<string, PresetShader> presets = new Dictionary<string, PresetShader>();
		private static Dictionary<int, string> idFileDict       = new Dictionary<int, string>();
		private static List<string> conflictList                = new List<string>();

		private static Dictionary<string, int> material_rq   = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
        private static Dictionary<string, Shader> PH_shaders = new Dictionary<string, Shader>();
        private static HoneyPot self;
    }
}
