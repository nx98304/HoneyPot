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

namespace ClassLibrary4
{
    [HarmonyPatch]
    public class HoneyPot : MonoBehaviour
    {
        private void Start()
        {
            self = GameObject.Find("HoneyPot").GetComponent<HoneyPot>();
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
            { "Standard",                       new string[] { "_Color", "_not_mapped_", "_Metallic", "_Glossiness", "_EmissionColor", "_not_mapped_", "_not_mapped_" } },
            { "Standard_Z",                     new string[] { "_Color", "_not_mapped_", "_Metallic", "_Glossiness", "_EmissionColor", "_not_mapped_", "_not_mapped_" } },
            { "Standard CustomMetallic",        new string[] { "_Color", "_SpecularColor", "_Metallic", "_Glossiness", "_EmissionColor", "_not_mapped_", "_not_mapped_" } },
            { "Standard_culloff",               new string[] { "_Color", "_not_mapped_", "_Metallic", "_Glossiness", "_EmissionColor", "_not_mapped_", "_not_mapped_" } },
            { "Standard_culloff_Z",             new string[] { "_Color", "_not_mapped_", "_Metallic", "_Glossiness", "_EmissionColor", "_not_mapped_", "_not_mapped_" } },
            { "Standard (Specular setup)",      new string[] { "_Color", "_SpecColor", "_not_mapped_", "_Glossiness", "_EmissionColor", "_not_mapped_", "_not_mapped_" } },
            { "Standard (Specular setup)_culloff", new string[] { "_Color", "_SpecColor", "_not_mapped_", "_Glossiness", "_EmissionColor", "_not_mapped_", "_not_mapped_" } },
            { "Standard_555",                   new string[] { "_Color", "_not_mapped_", "_Metallic", "_Glossiness", "_EmissionColor", "_not_mapped_", "_not_mapped_" } },
            { "HSStandard",                     new string[] { "_Color", "_SpecColor", "_Metallic", "_Smoothness", "_EmissionColor", "_not_mapped_", "_not_mapped_" } },
            { "HSStandard (Two Colors)",        new string[] { "_Color", "_SpecColor", "_Metallic", "_Smoothness", "_Color_3", "_SpecColor_3", "_SpecColor_3" } },
        };                                    //Note: the 7th idx is actually manipulating 6th's alpha value it seems, so only ones that runs TWO colors would need it...

        private void match_correct_shader_property(MaterialCustoms.Parameter mc, int idx, string shader_name)
        {
            if (shader_name == null || shader_name == "") return;

            if (MC_Mapping.ContainsKey(shader_name))
            {
                mc.propertyName = MC_Mapping[shader_name][idx];
            }
            else if (shader_name.Contains("HSStandard (Two Colors)")) //Note: match stricker rule first
            {
                mc.propertyName = MC_Mapping["HSStandard (Two Colors)"][idx];
            }
            else if (shader_name.Contains("HSStandard"))
            {
                mc.propertyName = MC_Mapping["HSStandard"][idx];
            }
            else if (shader_name.Contains("PBRsp_2layer"))
            {
                mc.propertyName = MC_Mapping["Shader Forge/PBRsp_2layer"][idx];
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
        static void Postfix(Hairs __instance, HairParameter param)
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
                                        this.logSave("This part of the hair mod seems to be non-transparent: " + HoneyPot.inspector[inspector_key] + ", default to " + HoneyPot.presets["PBRsp_3mask"].shader.name);
                                        material.shader = HoneyPot.presets["PBRsp_3mask"].shader;
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
            Info.ItemLoadInfo itemLoadInfo = Singleton<Info>.Instance.dicItemLoadInfo[__result.itemInfo.no];
            self.setItemShader(__result.objectItem, itemLoadInfo.bundlePath.Replace("\\", "/"));
            if (__result.isColor2 || __result.isChangeColor)
            {
                __result.UpdateColor();
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

        public void setAccsShader(Accessories acce, AccessoryParameter acceParam, int slot)
        {
            AccessoryCustom acceCustom = acceParam.slot[slot];
            AccessoryData accessoryData = CustomDataManager.GetAcceData(acceCustom.type, acceCustom.id);
            if (accessoryData == null)
            {
                return;
            }
            try
            {
                GameObject acceobj_obj = acce.objAcs[slot];
                Renderer[] renderers_in_acceobj = acceobj_obj.GetComponentsInChildren<Renderer>(true);
                string try_this_shader_name = "";
                List<string> list = new List<string>();
                foreach (Renderer r in renderers_in_acceobj)
                {
                    foreach (Material material in r.materials)
                    {
                        string material_name = material.name.Replace(" (Instance)", "");
                        string inspector_key = accessoryData.assetbundleName.Replace("\\", "/") + "|" + material_name;

                        if ("".Equals(material.shader.name))
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
                                    if (rq <= 2500)
                                    {
                                        this.logSave("Unable to map shader " + HoneyPot.inspector[inspector_key] + " to PH presets we have. Default to " + HoneyPot.presets["PBRsp_3mask"].shader.name);
                                        material.shader = HoneyPot.presets["PBRsp_3mask"].shader;
                                    }
                                    else
                                    {
                                        this.logSave("Unable to map shader " + HoneyPot.inspector[inspector_key] + " to PH presets we have. Default to " + HoneyPot.presets["Standard"].shader.name + " with high RQ to get transparency.");
                                        material.shader = HoneyPot.presets["Standard"].shader;
                                    }
                                }
                            }
                            material.renderQueue = rq;
                            //important: RQ assignment has to go after shader assignment. 
                            //           fucking implicit setter changes stuff... 
                        }

                        if (material.renderQueue <= 3600)
                        {   //Note: This is because we don't want to include glasses like accessories!!
                            //Note: It seems all glasses (transparent Standard) materials are around RQ 3800 or more
                            list.Add(material_name);
                            try_this_shader_name = material.shader.name;
                        }
                    }
                }
                MaterialCustoms materialCustoms = acceobj_obj.GetComponent<MaterialCustoms>();
                if (materialCustoms == null)
                {
                    this.logSave(" -- This accessory doesn't have MaterialCustoms, try adding one: " + accessoryData.assetbundleName.Replace("\\", "/") + ", shader: " + try_this_shader_name);
                    materialCustoms = acceobj_obj.AddComponent<MaterialCustoms>();
                    materialCustoms.parameters = new MaterialCustoms.Parameter[HoneyPot.mc.parameters.Length];
                    int idx = 0;
                    foreach (MaterialCustoms.Parameter copy in HoneyPot.mc.parameters)
                    {
                        materialCustoms.parameters[idx] = new MaterialCustoms.Parameter(copy);
                        match_correct_shader_property(materialCustoms.parameters[idx], idx, try_this_shader_name);
                        materialCustoms.parameters[idx++].materialNames = list.ToArray();
                    }
                    MaterialCustoms_Setup.Invoke(materialCustoms, new object[0]);
                }
                acce.UpdateColorCustom(slot);
            }
            catch (Exception ex)
            {
                this.logSave(ex.ToString());
            }
        }
        #endregion

        #region Wears shader remapping
        private static FieldInfo wearcustomedit_nowtabField = typeof(WearCustomEdit).GetField("nowTab", BindingFlags.Instance | BindingFlags.NonPublic);
        private static MethodInfo WearObj_SetupMaterials    = typeof(WearObj).GetMethod("SetupMaterials", new Type[] { typeof(WearData) });

        [HarmonyPatch(typeof(Wears), "WearInstantiate")]
        private static void Postfix(Wears __instance, WEAR_TYPE type, Material skinMaterial, Material customHighlightMat_Skin)
        {
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
                bool is_a_HS_cloth_parts_that_remmapped_shader = false;
                GameObject wearobj_obj = wearobj.obj;
                Renderer[] renderers_in_wearobj = wearobj_obj.GetComponentsInChildren<Renderer>(true);
                string try_this_shader_name = "";
                List<string> list = new List<string>();

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
                        bool flag = !renderer.name.Contains("_unc_") &&
                            !material.name.Contains("cf_m_body_CustomMaterial") &&
                            !material.name.Contains("cm_m_body_CustomMaterial") &&
                            "".Equals(material.shader.name);
                        if (//!renderer.name.Contains("_body_") &&
                            type != WEAR_TYPE.TOP || flag && !renderer.tag.Contains("New tag (8)") )
                        {
                            int rq = getRenderQueue(inspector_key, renderer.gameObject.GetComponent<SetRenderQueue>());
                            this.logSave("Wear material: " + inspector_key + " RQ: " + rq);
                            if ( HoneyPot.inspector.ContainsKey(inspector_key) )
                            {
                                if ( HoneyPot.presets.ContainsKey(HoneyPot.inspector[inspector_key]) )
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
                                    if (rq <= 2500)
                                    {
                                        this.logSave("Unable to map shader " + HoneyPot.inspector[inspector_key] + " to PH presets we have. Default to PBRsp_3mask as we find RQ value <= 2500.");
                                        material.shader = HoneyPot.presets["PBRsp_3mask"].shader;
                                    } 
                                    else
                                    {
                                        this.logSave("Unable to map shader " + HoneyPot.inspector[inspector_key] + " to PH presets we have. Default to PBRsp_3mask_alpha as we find RQ value > 2500.");
                                        material.shader = HoneyPot.presets["PBRsp_3mask_alpha"].shader;
                                    }
                                }
                            }
                            material.renderQueue = rq;
                            is_a_HS_cloth_parts_that_remmapped_shader = true;
                        }
                        if (((/*!renderer.name.Contains("_body_") &&*/
                             renderer.tag.Contains("ObjColor")) || forceColorable) && flag)
                        {
                            list.Add(material_name);
                            try_this_shader_name = material.shader.name;
                        }
                    }
                }

                if (is_a_HS_cloth_parts_that_remmapped_shader)
                {
                    MaterialCustoms materialCustoms = wearobj_obj.GetComponent<MaterialCustoms>();
                    if (materialCustoms == null && try_this_shader_name != "")
                    {
                        this.logSave(" -- This wear doesn't have MaterialCustoms, try adding one: " + wearData.assetbundleName.Replace("\\", "/") + ", shader: " + try_this_shader_name);
                        materialCustoms = wearobj_obj.AddComponent<MaterialCustoms>();
                        materialCustoms.parameters = new MaterialCustoms.Parameter[HoneyPot.mc.parameters.Length];
                        int k = 0;
                        foreach (MaterialCustoms.Parameter copy in HoneyPot.mc.parameters)
                        {
                            materialCustoms.parameters[k] = new MaterialCustoms.Parameter(copy);
                            match_correct_shader_property(materialCustoms.parameters[k], k, try_this_shader_name);
                            materialCustoms.parameters[k++].materialNames = list.ToArray();
                        }
                        MaterialCustoms_Setup.Invoke(materialCustoms, new object[0]);
                    }
                    WearObj_SetupMaterials.Invoke(wearobj, new object[]{ null }); //the WearData is never used/checked in this call
                    wearobj.UpdateColorCustom();
                    if( this.wearCustomEdit != null && (int)wearcustomedit_nowtabField.GetValue(this.wearCustomEdit) == idx )
                    {
                        // After a HS clothing is loaded, if wearCustomEdit is present and it is choosing the this wear slot
                        // Try to force the LoadedCoordinate() to enable color UI. Because before this point in time
                        // This HS clothing is deemed non-colorchangable because of its MaterialCustom is not set.
                        this.wearCustomEdit.LoadedCoordinate(type);
                    }
                }
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

            // Thank Doodoo for this!!!!!
            AssetBundle bundle_HSStandard_PH = loadEmbeddedAssetBundle("ClassLibrary4.hsstandardshaders");
            Material[] all_hsstandard_materials = bundle_HSStandard_PH.LoadAllAssets<Material>();
            foreach (Material m in all_hsstandard_materials)
            {
                if (!HoneyPot.PH_shaders.ContainsKey(m.shader.name))
                {
                    this.logSave("Found " + m.shader.name + " in hsstandardshaders2.");
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
        
		public void getListContent(string assetBundleDir, string fileName)
		{
			Dictionary<int, AccessoryData> dictionary = null;
			Dictionary<int, HairData> dictionary2 = null;
			Dictionary<int, BackHairData> dictionary3 = null;
			Dictionary<int, WearData> dictionary4 = null;
			Dictionary<int, WearData> dictionary5 = null;
			Dictionary<int, WearData> dictionary6 = null;
			Dictionary<int, WearData> female_swim_dict = null;
			Dictionary<int, WearData> dictionary8 = null;
			Dictionary<int, WearData> female_bra_dict = null;
			Dictionary<int, WearData> dictionary10 = null;
			Dictionary<int, WearData> dictionary11 = null;
			Dictionary<int, WearData> female_bot_dict = null;
			Dictionary<int, WearData> dictionary13 = null;
			Dictionary<int, WearData> female_top_dict = null;
			Dictionary<int, PrefabData> dictionary15 = null;
			Dictionary<int, PrefabData> dictionary16 = null;
			Dictionary<int, WearData> dictionary17 = null;
			Dictionary<int, WearData> dictionary18 = null;
            Dictionary<int, BackHairData> male_hair_dict = null;
            try
			{
				AssetBundle assetBundle = AssetBundle.LoadFromFile(assetBundleDir + "/" + fileName);
				foreach (TextAsset textAsset in assetBundle.LoadAllAssets<TextAsset>())
				{
					if (textAsset.name.Contains("ca_f_head"))
					{
						dictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.HEAD);
					}
					else if (textAsset.name.Contains("ca_f_hand"))
					{
						dictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.HAND);
					}
					else if (textAsset.name.Contains("ca_f_arm"))
					{
						dictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.ARM);
					}
					else if (textAsset.name.Contains("ca_f_back"))
					{
						dictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.BACK);
					}
					else if (textAsset.name.Contains("ca_f_breast"))
					{
						dictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.CHEST);
					}
					else if (textAsset.name.Contains("ca_f_ear"))
					{
						dictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.EAR);
					}
					else if (textAsset.name.Contains("ca_f_face"))
					{
						dictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.FACE);
					}
					else if (textAsset.name.Contains("ca_f_leg"))
					{
						dictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.LEG);
					}
					else if (textAsset.name.Contains("ca_f_megane"))
					{
						dictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.GLASSES);
					}
					else if (textAsset.name.Contains("ca_f_neck"))
					{
						dictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.NECK);
					}
					else if (textAsset.name.Contains("ca_f_shoulder"))
					{
						dictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.SHOULDER);
					}
					else if (textAsset.name.Contains("ca_f_waist"))
					{
						dictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.WAIST);
					}
					else if (textAsset.name.Contains("cf_f_hairB"))
					{
						dictionary3 = CustomDataManager.Hair_b;
					}
					else if (textAsset.name.Contains("cf_f_hairF"))
					{
						dictionary2 = CustomDataManager.Hair_f;
					}
					else if (textAsset.name.Contains("cf_f_hairS"))
					{
						dictionary2 = CustomDataManager.Hair_s;
					}
                    else if (textAsset.name.Contains("cm_f_hair"))
                    {
                        male_hair_dict = CustomDataManager.Hair_Male;
                    }
					else if (textAsset.name.Contains("cf_f_socks"))
					{
						dictionary4 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SOCKS);
					}
					else if (textAsset.name.Contains("cf_f_shoes"))
					{
						dictionary5 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SHOES);
					}
					else if (textAsset.name.Contains("cf_f_swimbot"))
					{
						dictionary6 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SWIM_BOTTOM);
					}
					else if (textAsset.name.Contains("cf_f_swimtop"))
					{
						dictionary8 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SWIM_TOP);
					}
					else if (textAsset.name.Contains("cf_f_swim"))
					{
						female_swim_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SWIM);
					}
					else if (textAsset.name.Contains("cf_f_bra"))
					{
						female_bra_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.BRA);
					}
					else if (textAsset.name.Contains("cf_f_shorts"))
					{
						dictionary10 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SHORTS);
					}
					else if (textAsset.name.Contains("cf_f_glove"))
					{
						dictionary11 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.GLOVE);
					}
					else if (textAsset.name.Contains("cf_f_panst"))
					{
						dictionary13 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.PANST);
					}
					else if (textAsset.name.Contains("cf_f_bot"))
					{
						female_bot_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.BOTTOM);
					}
					else if (textAsset.name.Contains("cf_f_top"))
					{
                        female_top_dict = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.TOP);
					}
					else if (textAsset.name.Contains("cf_m_eyebrow"))
					{
						dictionary15 = CustomDataManager.Eyebrow_Female;
					}
					else if (textAsset.name.Contains("cf_m_eyelashes"))
					{
						dictionary16 = CustomDataManager.Eyelash;
					}
					else if (textAsset.name.Contains("cm_f_body"))
					{
						dictionary17 = CustomDataManager.GetWearDictionary_Male(WEAR_TYPE.TOP);
					}
					else if (textAsset.name.Contains("cm_f_shoes"))
					{
						dictionary18 = CustomDataManager.GetWearDictionary_Male(WEAR_TYPE.SHOES);
					}
					if (dictionary18 != null)
					{
						string[] array2 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int j = 0; j < array2.Length; j++)
						{
							string[] array3 = array2[j].Split(new char[]
							{
								'\t'
							});
							if (array3.Length > 3)
							{
								try
								{
									int num = int.Parse(array3[0]) % 1000;
									if (array3[0].Length > 6)
									{
										num = int.Parse(array3[0]) % 1000000 + int.Parse(array3[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num += 838000;
									}
									WearData wearData = new WearData(num, array3[2], array3[4], array3[5], dictionary18.Count, false);
									wearData.id = num;
									if (!dictionary18.ContainsKey(wearData.id))
									{
										dictionary18.Add(wearData.id, wearData);
										HoneyPot.idFileDict[num] = array3[4];
									}
									else
									{
										this.addConflict(num, dictionary18[num].assetbundleName + "/" + dictionary18[num].prefab, wearData.assetbundleName + "/" + wearData.prefab, dictionary18[num].name, wearData.name);
									}
								}
								catch (Exception ex)
								{
									this.logSave(ex.ToString());
								}
							}
						}
					}
					if (dictionary17 != null)
					{
						string[] array4 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int k = 0; k < array4.Length; k++)
						{
							string[] array5 = array4[k].Split(new char[]
							{
								'\t'
							});
							if (array5.Length > 3)
							{
								try
								{
									int num2 = int.Parse(array5[0]) % 1000;
									if (array5[0].Length > 6)
									{
										num2 = int.Parse(array5[0]) % 1000000 + int.Parse(array5[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num2 += 837000;
									}
									WearData wearData2 = new WearData(num2, array5[2], array5[4], array5[5], dictionary17.Count, false);
									wearData2.id = num2;
									if (!dictionary17.ContainsKey(wearData2.id))
									{
										dictionary17.Add(wearData2.id, wearData2);
										HoneyPot.idFileDict[num2] = array5[4];
									}
									else
									{
										this.addConflict(num2, dictionary17[num2].assetbundleName + "/" + dictionary17[num2].prefab, wearData2.assetbundleName + "/" + wearData2.prefab, dictionary17[num2].name, wearData2.name);
									}
								}
								catch (Exception ex2)
								{
									this.logSave(ex2.ToString());
								}
							}
						}
					}
					if (dictionary15 != null)
					{
						string[] array6 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int l = 0; l < array6.Length; l++)
						{
							string[] array7 = array6[l].Split(new char[]
							{
								'\t'
							});
							if (array7.Length > 3)
							{
								try
								{
									int num3 = int.Parse(array7[0]) % 1000;
									if (array7[0].Length > 6)
									{
										num3 = int.Parse(array7[0]) % 1000000 + int.Parse(array7[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num3 += 835000;
									}
									PrefabData prefabData = new PrefabData(num3, array7[2], array7[4], array7[5], dictionary15.Count, false);
									prefabData.id = num3;
									if (!dictionary15.ContainsKey(prefabData.id))
									{
										dictionary15.Add(prefabData.id, prefabData);
										HoneyPot.idFileDict[num3] = array7[4];
									}
									else
									{
										this.addConflict(num3, dictionary15[num3].assetbundleName + "/" + dictionary15[num3].prefab, prefabData.assetbundleName + "/" + prefabData.prefab, dictionary15[num3].name, prefabData.name);
									}
								}
								catch (Exception ex3)
								{
									this.logSave(ex3.ToString());
								}
							}
						}
					}
					if (dictionary16 != null)
					{
						string[] array8 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int m = 0; m < array8.Length; m++)
						{
							string[] array9 = array8[m].Split(new char[]
							{
								'\t'
							});
							if (array9.Length > 3)
							{
								try
								{
									int num4 = int.Parse(array9[0]) % 1000;
									if (array9[0].Length > 6)
									{
										num4 = int.Parse(array9[0]) % 1000000 + int.Parse(array9[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num4 += 836000;
									}
									PrefabData prefabData2 = new PrefabData(num4, array9[2], array9[4], array9[5], dictionary16.Count, false);
									prefabData2.id = num4;
									if (!dictionary16.ContainsKey(prefabData2.id))
									{
										dictionary16.Add(prefabData2.id, prefabData2);
										HoneyPot.idFileDict[num4] = array9[4];
									}
									else
									{
										this.addConflict(num4, dictionary16[num4].assetbundleName + "/" + dictionary16[num4].prefab, prefabData2.assetbundleName + "/" + prefabData2.prefab, dictionary16[num4].name, prefabData2.name);
									}
								}
								catch (Exception ex4)
								{
									this.logSave(ex4.ToString());
								}
							}
						}
					}
					if (female_bot_dict != null)
					{
						string[] array10 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int n = 0; n < array10.Length; n++)
						{
							string[] celldata = array10[n].Split(new char[]
							{
								'\t'
							});
							if (celldata.Length > 3)
							{
								try
								{
									int num5 = int.Parse(celldata[0]) % 1000;
									if (celldata[0].Length > 6)
									{
										num5 = int.Parse(celldata[0]) % 1000000 + int.Parse(celldata[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num5 += 821000;
									}
									WearData wearData = new WearData(num5, celldata[2], celldata[4], celldata[6], female_bot_dict.Count, false);
                                    wearData.id = num5;
									if (!female_bot_dict.ContainsKey(wearData.id))
									{
                                        wearData.liquid = celldata[11];
                                        wearData.coordinates = int.Parse(celldata[14]);
                                        wearData.shortsDisable = (!celldata[16].Equals("0"));

                                        female_bot_dict.Add(wearData.id, wearData);
										HoneyPot.idFileDict[num5] = celldata[4];
									}
									else
									{
										this.addConflict(num5, female_bot_dict[num5].assetbundleName + "/" + female_bot_dict[num5].prefab, wearData.assetbundleName + "/" + wearData.prefab, female_bot_dict[num5].name, wearData.name);
									}
								}
								catch (Exception ex5)
								{
									this.logSave(ex5.ToString());
								}
							}
						}
					}
					if (female_top_dict != null)
					{
						string[] array12 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num6 = 0; num6 < array12.Length; num6++)
						{
							string[] celldata = array12[num6].Split(new char[]
							{
								'\t'
							});
							if (celldata.Length > 3)
							{
								try
								{
									int num7 = int.Parse(celldata[0]) % 1000;
									if (celldata[0].Length > 6)
									{
										num7 = int.Parse(celldata[0]) % 1000000 + int.Parse(celldata[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num7 += 820000;
									}
									WearData wearData = new WearData(num7, celldata[2], celldata[4], celldata[6], female_top_dict.Count, false);
                                    wearData.id = num7;
									if (!female_top_dict.ContainsKey(wearData.id))
									{
                                        wearData.liquid = celldata[11];
                                        wearData.coordinates = int.Parse(celldata[14]);
//                                        wearData.shortsDisable = (celldata[14].Equals("2"));
                                        wearData.braDisable = (!celldata[15].Equals("0"));
                                        wearData.nip = (!celldata[17].Equals("0"));

                                        female_top_dict.Add(wearData.id, wearData);
										HoneyPot.idFileDict[num7] = celldata[4];
									}
									else
									{
										this.addConflict(num7, female_top_dict[num7].assetbundleName + "/" + female_top_dict[num7].prefab, wearData.assetbundleName + "/" + wearData.prefab, female_top_dict[num7].name, wearData.name);
									}
								}
								catch (Exception ex6)
								{
									this.logSave(ex6.ToString());
								}
							}
						}
					}
					if (dictionary13 != null)
					{
						string[] array14 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num8 = 0; num8 < array14.Length; num8++)
						{
							string[] array15 = array14[num8].Split(new char[]
							{
								'\t'
							});
							if (array15.Length > 3)
							{
								try
								{
									int num9 = int.Parse(array15[0]) % 1000;
									if (array15[0].Length > 6)
									{
										num9 = int.Parse(array15[0]) % 1000000 + int.Parse(array15[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num9 += 828000;
									}
									WearData wearData5 = new WearData(num9, array15[2], array15[4], array15[6], dictionary13.Count, false);
									wearData5.id = num9;
									if (!dictionary13.ContainsKey(wearData5.id))
									{
										dictionary13.Add(wearData5.id, wearData5);
										HoneyPot.idFileDict[num9] = array15[4];
									}
									else
									{
										this.addConflict(num9, dictionary13[num9].assetbundleName + "/" + dictionary13[num9].prefab, wearData5.assetbundleName + "/" + wearData5.prefab, dictionary13[num9].name, wearData5.name);
									}
								}
								catch (Exception ex7)
								{
									this.logSave(ex7.ToString());
								}
							}
						}
					}
					if (dictionary11 != null)
					{
						string[] array16 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num10 = 0; num10 < array16.Length; num10++)
						{
							string[] array17 = array16[num10].Split(new char[]
							{
								'\t'
							});
							if (array17.Length > 3)
							{
								try
								{
									int num11 = int.Parse(array17[0]) % 1000;
									if (array17[0].Length > 6)
									{
										num11 = int.Parse(array17[0]) % 1000000 + int.Parse(array17[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num11 += 827000;
									}
									WearData wearData6 = new WearData(num11, array17[2], array17[4], array17[6], dictionary11.Count, false);
									wearData6.id = num11;
									if (!dictionary11.ContainsKey(wearData6.id))
									{
										dictionary11.Add(wearData6.id, wearData6);
										HoneyPot.idFileDict[num11] = array17[4];
									}
									else
									{
										this.addConflict(num11, dictionary11[num11].assetbundleName + "/" + dictionary11[num11].prefab, wearData6.assetbundleName + "/" + wearData6.prefab, dictionary11[num11].name, wearData6.name);
									}
								}
								catch (Exception ex8)
								{
									this.logSave(ex8.ToString());
								}
							}
						}
					}
					if (dictionary10 != null)
					{
						string[] array18 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num12 = 0; num12 < array18.Length; num12++)
						{
							string[] array19 = array18[num12].Split(new char[]
							{
								'\t'
							});
							if (array19.Length > 3)
							{
								try
								{
									int num13 = int.Parse(array19[0]) % 1000;
									if (array19[0].Length > 6)
									{
										num13 = int.Parse(array19[0]) % 1000000 + int.Parse(array19[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num13 += 823000;
									}
									WearData wearData7 = new WearData(num13, array19[2], array19[4], array19[6], dictionary10.Count, false);
									wearData7.id = num13;
									if (!dictionary10.ContainsKey(wearData7.id))
									{
                                        wearData7.liquid = array19[11];

                                        dictionary10.Add(wearData7.id, wearData7);
										HoneyPot.idFileDict[num13] = array19[4];
									}
									else
									{
										this.addConflict(num13, dictionary10[num13].assetbundleName + "/" + dictionary10[num13].prefab, wearData7.assetbundleName + "/" + wearData7.prefab, dictionary10[num13].name, wearData7.name);
									}
								}
								catch (Exception ex9)
								{
									this.logSave(ex9.ToString());
								}
							}
						}
					}
					if (female_bra_dict != null)
					{
						string[] array20 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num14 = 0; num14 < array20.Length; num14++)
						{
							string[] celldata = array20[num14].Split(new char[]
							{
								'\t'
							});
							if (celldata.Length > 3)
							{
								try
								{
									int num15 = int.Parse(celldata[0]) % 1000;
									if (celldata[0].Length > 6)
									{
										num15 = int.Parse(celldata[0]) % 1000000 + int.Parse(celldata[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num15 += 822000;
									}
									WearData wearData = new WearData(num15, celldata[2], celldata[4], celldata[6], female_bra_dict.Count, false);
                                    wearData.id  = num15;
                                    wearData.nip = false; // NOTE: Curious. PH now activates nipple when Bra is shown regardless of this setting?
                                                          //       No nipple actually shows through though. So all fine?
                                    WearData wearData9 = female_bra_dict[1];
									if (!female_bra_dict.ContainsKey(wearData.id))
									{
                                        wearData.liquid = celldata[11];

                                        female_bra_dict.Add(wearData.id, wearData);
										HoneyPot.idFileDict[num15] = celldata[4];
									}
									else
									{
										this.addConflict(num15, female_bra_dict[num15].assetbundleName + "/" + female_bra_dict[num15].prefab, wearData.assetbundleName + "/" + wearData.prefab, female_bra_dict[num15].name, wearData.name);
									}
								}
								catch (Exception ex10)
								{
									this.logSave(ex10.ToString());
								}
							}
						}
					}
					if (dictionary8 != null)
					{
						string[] array22 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num16 = 0; num16 < array22.Length; num16++)
						{
							string[] array23 = array22[num16].Split(new char[]
							{
								'\t'
							});
							if (array23.Length > 3)
							{
								try
								{
									int num17 = int.Parse(array23[0]) % 1000;
									if (array23[0].Length > 6)
									{
										num17 = int.Parse(array23[0]) % 1000000 + int.Parse(array23[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num17 += 825000;
									}
									WearData wearData10 = new WearData(num17, array23[2], array23[4], array23[6], dictionary8.Count, false);
									wearData10.id = num17;
									if (!dictionary8.ContainsKey(wearData10.id))
									{
                                        wearData10.liquid = array23[11];

                                        dictionary8.Add(wearData10.id, wearData10);
										HoneyPot.idFileDict[num17] = array23[4];
									}
									else
									{
										this.addConflict(num17, dictionary8[num17].assetbundleName + "/" + dictionary8[num17].prefab, wearData10.assetbundleName + "/" + wearData10.prefab, dictionary8[num17].name, wearData10.name);
									}
								}
								catch (Exception ex11)
								{
									this.logSave(ex11.ToString());
								}
							}
						}
					}
					if (female_swim_dict != null)
					{
						string[] array24 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num18 = 0; num18 < array24.Length; num18++)
						{
							string[] celldata = array24[num18].Split(new char[]
							{
								'\t'
							});
							if (celldata.Length > 3)
							{
								try
								{
									int num19 = int.Parse(celldata[0]) % 1000;
									if (celldata[0].Length > 6)
									{
										num19 = int.Parse(celldata[0]) % 1000000 + int.Parse(celldata[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num19 += 824000;
									}
									WearData wearData = new WearData(num19, celldata[2], celldata[4], celldata[6], female_swim_dict.Count, false);
                                    wearData.id = num19;
									if (!female_swim_dict.ContainsKey(wearData.id))
									{
                                        // TODO: How do we specify that this swimsuit is top-bottom separated or not?
                                        wearData.liquid = celldata[11];

                                        female_swim_dict.Add(wearData.id, wearData);
										HoneyPot.idFileDict[num19] = celldata[4];
									}
									else
									{
										this.addConflict(num19, female_swim_dict[num19].assetbundleName + "/" + female_swim_dict[num19].prefab, wearData.assetbundleName + "/" + wearData.prefab, female_swim_dict[num19].name, wearData.name);
									}
								}
								catch (Exception ex12)
								{
									this.logSave(ex12.ToString());
								}
							}
						}
					}
					if (dictionary6 != null)
					{
						string[] array26 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num20 = 0; num20 < array26.Length; num20++)
						{
							string[] array27 = array26[num20].Split(new char[]
							{
								'\t'
							});
							if (array27.Length > 3)
							{
								try
								{
									int num21 = int.Parse(array27[0]) % 1000;
									if (array27[0].Length > 6)
									{
										num21 = int.Parse(array27[0]) % 1000000 + int.Parse(array27[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num21 += 826000;
									}
									WearData wearData12 = new WearData(num21, array27[2], array27[4], array27[6], dictionary6.Count, false);
									wearData12.id = num21;
									if (!dictionary6.ContainsKey(wearData12.id))
									{
                                        wearData12.liquid = array27[11];

										dictionary6.Add(wearData12.id, wearData12);
										HoneyPot.idFileDict[num21] = array27[4];
									}
									else
									{
										this.addConflict(num21, dictionary6[num21].assetbundleName + "/" + dictionary6[num21].prefab, wearData12.assetbundleName + "/" + wearData12.prefab, dictionary6[num21].name, wearData12.name);
									}
								}
								catch (Exception ex13)
								{
									this.logSave(ex13.ToString());
								}
							}
						}
					}
					if (dictionary5 != null)
					{
						string[] array28 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num22 = 0; num22 < array28.Length; num22++)
						{
							string[] array29 = array28[num22].Split(new char[]
							{
								'\t'
							});
							if (array29.Length > 3)
							{
								try
								{
									int num23 = int.Parse(array29[0]) % 1000;
									if (array29[0].Length > 6)
									{
										num23 = int.Parse(array29[0]) % 1000000 + int.Parse(array29[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num23 += 830000;
									}
									WearData wearData13 = new WearData(num23, array29[2], array29[4], array29[6], dictionary5.Count, false);
									wearData13.id = num23;
									if (!dictionary5.ContainsKey(wearData13.id))
									{
										dictionary5.Add(wearData13.id, wearData13);
										HoneyPot.idFileDict[num23] = array29[4];
									}
									else
									{
										this.addConflict(num23, dictionary5[num23].assetbundleName + "/" + dictionary5[num23].prefab, wearData13.assetbundleName + "/" + wearData13.prefab, dictionary5[num23].name, wearData13.name);
									}
								}
								catch (Exception ex14)
								{
									this.logSave(ex14.ToString());
								}
							}
						}
					}
					if (dictionary4 != null)
					{
						string[] array30 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num24 = 0; num24 < array30.Length; num24++)
						{
							string[] array31 = array30[num24].Split(new char[]
							{
								'\t'
							});
							if (array31.Length > 3)
							{
								try
								{
									int num25 = int.Parse(array31[0]) % 1000;
									if (array31[0].Length > 6)
									{
										num25 = int.Parse(array31[0]) % 1000000 + int.Parse(array31[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num25 += 829000;
									}
									WearData wearData14 = new WearData(num25, array31[2], array31[4], array31[6], dictionary4.Count, false);
									wearData14.id = num25;
									if (!dictionary4.ContainsKey(wearData14.id))
									{
										dictionary4.Add(wearData14.id, wearData14);
										HoneyPot.idFileDict[num25] = array31[4];
									}
									else
									{
										this.addConflict(num25, dictionary4[num25].assetbundleName + "/" + dictionary4[num25].prefab, wearData14.assetbundleName + "/" + wearData14.prefab, dictionary4[num25].name, wearData14.name);
									}
								}
								catch (Exception ex15)
								{
									this.logSave(ex15.ToString());
								}
							}
						}
					}
					if (dictionary != null)
					{
						string[] array32 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num26 = 0; num26 < array32.Length; num26++)
						{
							string[] array33 = array32[num26].Split(new char[]
							{
								'\t'
							});
							if (array33.Length > 3)
							{
								try
								{
									int num27 = int.Parse(array33[0]) % 1000;
									if (array33[0].Length > 6)
									{
										num27 = int.Parse(array33[0]) % 1000000 + int.Parse(array33[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num27 += 832000;
									}
									AccessoryData accessoryData = new AccessoryData(num27, array33[2], array33[4], array33[5], array33[6], array33[8], ItemDataBase.SPECIAL.NONE, dictionary.Count, false);
									accessoryData.id = num27;
									if (!dictionary.ContainsKey(accessoryData.id))
									{
										dictionary.Add(accessoryData.id, accessoryData);
										HoneyPot.idFileDict[num27] = array33[4];
									}
									else
									{
										this.addConflict(num27, dictionary[num27].assetbundleName + "/" + dictionary[num27].prefab_F, accessoryData.assetbundleName + "/" + accessoryData.prefab_F, dictionary[num27].name, accessoryData.name);
									}
								}
								catch (Exception ex16)
								{
									this.logSave(ex16.ToString());
								}
							}
						}
					}
					if (dictionary2 != null)
					{
						string[] array34 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num28 = 0; num28 < array34.Length; num28++)
						{
							string[] array35 = array34[num28].Split(new char[]
							{
								'\t'
							});
							if (array35.Length > 3)
							{
								try
								{
									int num29 = int.Parse(array35[0]) % 1000;
									if (array35[0].Length > 6)
									{
										num29 = int.Parse(array35[0]) % 1000000 + int.Parse(array35[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num29 += 833000;
									}
									HairData hairData = new HairData(num29, array35[2], array35[4], array35[6], dictionary2.Count, false);
									hairData.id = num29;
									if (!dictionary2.ContainsKey(hairData.id))
									{
										dictionary2.Add(hairData.id, hairData);
										HoneyPot.idFileDict[num29] = array35[4];
									}
									else
									{
										this.addConflict(num29, dictionary2[num29].assetbundleName + "/" + dictionary2[num29].prefab, hairData.assetbundleName + "/" + hairData.prefab, dictionary2[num29].name, hairData.name);
									}
								}
								catch (Exception ex17)
								{
									this.logSave(ex17.ToString());
								}
							}
						}
					}
					if (dictionary3 != null)
					{
						string[] array36 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num30 = 0; num30 < array36.Length; num30++)
						{
							string[] array37 = array36[num30].Split(new char[]
							{
								'\t'
							});
							if (array37.Length > 3)
							{
								try
								{
									int num31 = int.Parse(array37[0]) % 1000;
									if (array37[0].Length > 6)
									{
										num31 = int.Parse(array37[0]) % 1000000 + int.Parse(array37[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num31 += 834000;
									}
									BackHairData backHairData = new BackHairData(num31, array37[2], array37[4], array37[6], dictionary3.Count, false, "セミロング", "1".Equals(array37[13]));
									backHairData.id = num31;
									if (!dictionary3.ContainsKey(backHairData.id))
									{
										dictionary3.Add(backHairData.id, backHairData);
										HoneyPot.idFileDict[num31] = array37[4];
									}
									else
									{
										this.addConflict(num31, dictionary3[num31].assetbundleName + "/" + dictionary3[num31].prefab, backHairData.assetbundleName + "/" + backHairData.prefab, dictionary3[num31].name, backHairData.name);
									}
								}
								catch (Exception ex18)
								{
									this.logSave(ex18.ToString());
								}
							}
						}
					}
                    if (male_hair_dict != null)
                    {
                        string[] list = textAsset.text.Replace("\r\n", "\n").Split(new char[]
                        {
                            '\n'
                        });
                        for (int i = 0; i < list.Length; i++)
                        {
                            string[] celldata = list[i].Split(new char[]
                            {
                                '\t'
                            });
                            if (celldata.Length > 3)
                            {
                                try
                                {
                                    int id = int.Parse(celldata[0]) % 1000;
                                    if (celldata[0].Length > 6)
                                    {
                                        id = int.Parse(celldata[0]) % 1000000 + int.Parse(celldata[0].Substring(0, 3)) * 1000;
                                    }
                                    else
                                    {
                                        id += 839000;
                                    }
                                    //Male Hair is ALWAYS set?
                                    BackHairData backHairData = new BackHairData(id, celldata[2], celldata[4], celldata[5], male_hair_dict.Count, false, "セミロング", true);
                                    backHairData.id = id;
                                    if (!male_hair_dict.ContainsKey(backHairData.id))
                                    {
                                        male_hair_dict.Add(backHairData.id, backHairData);
                                        HoneyPot.idFileDict[id] = celldata[4];
                                    }
                                    else
                                    {
                                        this.addConflict(id, male_hair_dict[id].assetbundleName + "/" + male_hair_dict[id].prefab, backHairData.assetbundleName + "/" + backHairData.prefab, male_hair_dict[id].name, backHairData.name);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    this.logSave(ex.ToString());
                                }
                            }
                        }
                    }
                    dictionary = null;
					dictionary2 = null;
					dictionary3 = null;
					dictionary4 = null;
					dictionary5 = null;
					dictionary6 = null;
                    female_swim_dict = null;
					dictionary8 = null;
                    female_bra_dict = null;
					dictionary10 = null;
					dictionary11 = null;
                    female_bot_dict = null;
					dictionary13 = null;
                    female_top_dict = null;
					dictionary15 = null;
					dictionary16 = null;
					dictionary17 = null;
					dictionary18 = null;
                    male_hair_dict = null;

                }
				assetBundle.Unload(true);
			}
			catch (Exception ex19)
			{
				this.logSave(ex19.ToString());
			}
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
        }
        #endregion

        private void Update()
		{
			if (HoneyPot.isFirst)
			{
                System.Diagnostics.Stopwatch t = new System.Diagnostics.Stopwatch();
                t.Start();
                this.readAllPHShaders();
                this.loadShaderMapping();
                this.logSave("Shader loaded timestamp: " + t.ElapsedMilliseconds.ToString());
				try
				{
					foreach (FileInfo fileInfo in new DirectoryInfo(assetBundlePath + "/list/characustom").GetFiles())
					{
						this.getListContent(assetBundlePath, "list/characustom/" + fileInfo.Name);
					}
				}
				catch (Exception ex)
				{
					this.logSave(ex.ToString());
				}
                this.logSave("List crawled timestamp: " + t.ElapsedMilliseconds.ToString());
				this.readInspector();
                this.logSave("Inspector established timestamp: " + t.ElapsedMilliseconds.ToString());
                this.exportConflict();
				this.transportDicts();
                this.logSave("Move categories timestamp: " + t.ElapsedMilliseconds.ToString());
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
                this.wearCustomEdit = UnityEngine.Object.FindObjectOfType<WearCustomEdit>();
			}
		}

        #region NotReallyRelated_To_HoneyPot: Mannequin BoneWeight fix if you changed the resources.assets FemaleBody
        //TODO: To_be_moved: This should be moved out of HoneyPot when I have time. 
        //      Should really be combined with the body mesh NML fixes that I hardcoded in Assembly-CSharp.dll
        private static Human reference_to_human = null;
        private static FieldInfo Wears_bodySkinMeshField = typeof(Wears).GetField("bodySkinMesh", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPatch(typeof(CoordinateCapture), "SetHuman")]
        private static void Prefix(Human human)
        {
            reference_to_human = human;
        }

        [HarmonyPatch(typeof(SyncBoneWeight), "Awake")]
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
                for (int i = 0; i < r.bones.Length; i++)
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
