using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Character;
using IllusionPlugin;
using Studio;
using UnityEngine;

namespace ClassLibrary4
{
	// Token: 0x02000007 RID: 7
	public class HoneyPot : MonoBehaviour
	{
		// Token: 0x06000022 RID: 34 RVA: 0x000024E0 File Offset: 0x000006E0
		private void Start()
		{
			this.hairObjField = this.typeHairs.GetField("parts", BindingFlags.Instance | BindingFlags.NonPublic);
			this.libAssembly = Assembly.GetAssembly(typeof(Hairs));
			this.typeHairObj = this.libAssembly.GetType("HairObj");
			this.gameObjField = this.typeHairObj.GetField("obj", BindingFlags.Instance | BindingFlags.Public);
			this.nowTabField = this.typeWearCustomEdit.GetField("nowTab", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002560 File Offset: 0x00000760
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

		// Token: 0x06000024 RID: 36 RVA: 0x00002638 File Offset: 0x00000838
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

		// Token: 0x06000025 RID: 37 RVA: 0x00002714 File Offset: 0x00000914
		private void readInspector()
		{
			StreamReader streamReader = new StreamReader(this.inspectorText);
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
						this.inspector[key] = value;
						HoneyPot.material_rq[key] = value2;
					}
				}
				catch (Exception)
				{
				}
			}
			streamReader.Close();
		}

		// Token: 0x06000026 RID: 38 RVA: 0x000027B0 File Offset: 0x000009B0
		public void loadDefaultHairShader()
		{
			try
			{
				GameObject gameObject = AssetBundleLoader.LoadAndInstantiate<GameObject>(this.assetBundlePath, this.getHairAssetBundleName(), this.getHairPrefabName());
				Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(true);
				this.orgShader = componentsInChildren[0].materials[0].shader;
                gameObject.SetActive(false); 
				GameObject gameObject2 = AssetBundleLoader.LoadAndInstantiate<GameObject>(this.assetBundlePath, "wear/cf_top_hsad", "p_cf_yayoi_top");
                Renderer[] componentsInChildren2 = gameObject2.GetComponentsInChildren<Renderer>(true);
                this.mats = componentsInChildren2[0].materials;
                gameObject2.SetActive(false);
                this.mc = gameObject2.GetComponentInChildren<MaterialCustoms>();
                foreach (KeyValuePair<string, PresetShader> keyValuePair in this.presets)
				{
					PresetShader value = keyValuePair.Value;
					if (value.assetBundleName.Contains("cf_m_"))
					{
						Material material = AssetBundleLoader.LoadAndInstantiate<Material>(this.assetBundlePath, value.assetBundleName, value.assetName);
						value.material = material;
						value.shader = value.material.shader;
					}
					else
					{
						GameObject gameObject3 = AssetBundleLoader.LoadAndInstantiate<GameObject>(this.assetBundlePath, value.assetBundleName, value.assetName);
						Renderer[] componentsInChildren3 = gameObject3.GetComponentsInChildren<Renderer>(true);
						value.material = componentsInChildren3[value.rendererIdx].materials[value.materialIdx];
						value.shader = value.material.shader;
						gameObject3.SetActive(false);
                    }
                }

                // Adding two specific PresetShader only for simple particle effects: 
                if (!this.presets.ContainsKey("Particle Add"))
                {
                    this.presets.Add("Particle Add", new PresetShader());
                    this.presets["Particle Add"].shader = Shader.Find("Particles/Additive");
                }
                if (!this.presets.ContainsKey("Particle Alpha Blend"))
                {
                    this.presets.Add("Particle Alpha Blend", new PresetShader());
                    this.presets["Particle Alpha Blend"].shader = Shader.Find("Particles/Alpha Blended");
                }
			}
			catch (Exception ex)
			{
				this.logSave(ex.ToString());
			}
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002A14 File Offset: 0x00000C14
		public List<string> readOldPresetShaderString()
		{
			List<string> list = new List<string>();
			string str1 = ModPrefs.GetString("HoneyPot", "PresetShader_000", "accessory/ca_megane_hs00|p_asc_glasses_11_00|0|0|0", false);
			string str2 = ModPrefs.GetString("HoneyPot", "PresetShader_001", "wear/cf_bra_hsad|p_cf_bra_09_00|0|0|0", false);
			string str3 = ModPrefs.GetString("HoneyPot", "PresetShader_002", "wear/cf_top_hsad|p_cf_yayoi_top|1|0|0", false);
			string str4 = ModPrefs.GetString("HoneyPot", "PresetShader_003", "wear/cf_top_hsad|p_cf_top_idol1_00|1|0|0", false);
			string str5 = ModPrefs.GetString("HoneyPot", "PresetShader_004", "wear/cf_top_hsad|p_cf_top_miko1|0|0|0", false);
			string str6 = ModPrefs.GetString("HoneyPot", "PresetShader_005", "", false);
			string str7 = ModPrefs.GetString("HoneyPot", "PresetShader_006", "", false);
			list.Add(str1);
			list.Add(str2);
			list.Add(str3);
			list.Add(str4);
			list.Add(str5);
			if (!string.IsNullOrEmpty(str6))
			{
				this.SHADER_NORMAL_MAX = 5;
				list.Add(str6);
				if (!string.IsNullOrEmpty(str7))
				{
					this.SHADER_NORMAL_MAX = 6;
					list.Add(str7);
				}
			}
			return list;
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002B1C File Offset: 0x00000D1C
		public void loadOldPreset()
		{
			try
			{
				foreach (string text in this.readOldPresetShaderString())
				{
					if (text.Length >= 2)
					{
						string[] array = text.Split(new char[]
						{
							'|'
						});
						if (array.Length >= 5)
						{
							PresetShader presetShader = new PresetShader();
							presetShader.assetBundleName = array[0];
							presetShader.assetName = array[1];
							presetShader.rendererIdx = int.Parse(array[2]);
							presetShader.materialIdx = int.Parse(array[3]);
							presetShader.doCopyMaterial = array[4];
							this.presets.Add(array[0] + "|" + array[1], presetShader);
							this.presetKeys.Add(array[0] + "|" + array[1]);
						}
					}
				}
			}
			catch (Exception ex)
			{
				this.logSave(ex.ToString());
			}
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002C28 File Offset: 0x00000E28
		public void setHairShaders()
		{
			foreach (Female female in this.currentFemaleList)
			{
				if (female.isActiveAndEnabled)
				{
					Hairs hairs = female.hairs;
					this.setHairShaders(hairs, female);
				}
			}
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002C68 File Offset: 0x00000E68
		public void setItemShaders()
		{
			foreach (KeyValuePair<int, ObjectCtrlInfo> keyValuePair in Singleton<Studio.Studio>.Instance.dicObjectCtrl)
			{
				OCIItem item = keyValuePair.Value as OCIItem;
                if (item != null)
                {
                    this.setItemShader(item);
                }
			}
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002CD8 File Offset: 0x00000ED8
		public void setHairShaders(Hairs hairs, Human h)
		{
			Array array = (Array)this.hairObjField.GetValue(hairs);
			for (int i = 0; i < array.Length; i++)
			{
                if (array.GetValue(i) != null)
                {
                    int id = h.customParam.hair.parts[i].ID;
                    HairData hair_Female = CustomDataManager.GetHair_Female((HAIR_TYPE)i, id);
                    this.setHairShaderObj(array.GetValue(i), hair_Female.assetbundleName);
                }
			}
			h.hairs.ChangeColor(h.customParam.hair);
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002D54 File Offset: 0x00000F54
		public Shader getShader(int id, string materialName)
		{
			string fileName = this.getFileName(id);
			return this.getShader(fileName, materialName.Replace(" (Instance)", ""), false, false);
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002D84 File Offset: 0x00000F84
		public Shader getShader(string fileName, string materialName, bool isItem = false, bool isTrans = false)
		{
			this.logSave(string.Concat(new object[]
			{
				"getShader : ",
				fileName,
				" ",
				materialName,
				" ",
				this.inspector.Count
			}));
			string text = "PBRsp_3mask";
			if (isItem)
			{
				text = "deffuse";
			}
			if (this.inspector.ContainsKey(fileName + "|" + materialName))
			{
				text = this.inspector[fileName + "|" + materialName];
				this.logSave("contains:" + text);
				if (text.Equals(""))
				{
					text = "deffuse";
				}
			}
			if (!this.presets.ContainsKey(text))
			{
				this.logSave("deffuse");
				if (!isTrans && isItem)
				{
					text = "deffuse";
				}
				else
				{
					text = "_";
				}
			}
			if (this.presets.ContainsKey(text))
			{
				return this.presets[text].shader;
			}
			return Shader.Find("Diffuse");
		}

		// Token: 0x0600002E RID: 46 RVA: 0x000021CA File Offset: 0x000003CA
		public string getFileName(int id)
		{
			if (HoneyPot.idFileDict.ContainsKey(id))
			{
				return HoneyPot.idFileDict[id];
			}
			return "";
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00002E94 File Offset: 0x00001094
		public void setHairShaderObj(object temp, string assetBundleName)
		{
			try
			{
				GameObject gameObject = (GameObject)this.gameObjField.GetValue(temp);
				if (gameObject.GetComponent<Destroyer>() == null)
				{
					gameObject.AddComponent<Destroyer>();
				}
				Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(true);
				SetRenderQueue[] componentsInChildren2 = gameObject.GetComponentsInChildren<SetRenderQueue>(true);
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					foreach (Material material in componentsInChildren[i].materials)
					{
						if (this.orgShader == null && !"".Equals(material.shader.name))
						{
							this.orgShader = material.shader;
						}
						if (this.orgShader != null && "".Equals(material.shader.name))
						{
							if (material.renderQueue <= 2500)
							{
								PresetShader presetShader = this.presets[this.presetKeys[this.SHADER_NORMAL_1]];
								material.shader = presetShader.shader;
							}
							else
							{
								material.shader = this.orgShader;
							}
							material.shader = this.orgShader;
							material.renderQueue += 100;
							string key = (assetBundleName + "|" + material.name).Replace(" (Instance)", "");
							if (HoneyPot.material_rq.ContainsKey(key))
							{
								material.renderQueue = HoneyPot.material_rq[key];
							}
							if (i < componentsInChildren2.Length)
							{
								int[] array = componentsInChildren2[i].Get();
								if (array.Length != 0)
								{
									material.renderQueue = array[0];
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				this.logSave(ex.ToString());
			}
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00003070 File Offset: 0x00001270
		public void setAccsShaders()
		{
			foreach (Female female in this.currentFemaleList)
			{
				if (female.isActiveAndEnabled)
				{
					this.setAccsShader(false, female);
                    this.setWearShader(female, 9, WEAR_TYPE.SOCKS, 3000, 3000, 1, false, false);
                    this.setWearShader(female, 10, WEAR_TYPE.SHOES, 3000, 3000, 1, false, false);
                    this.setWearShader(female, 6, WEAR_TYPE.SWIM_BOTTOM, 3000, 3000, 1, false, false);
                    this.setWearShader(female, 4, WEAR_TYPE.SWIM, 3000, 3000, 1, false, false);
                    this.setWearShader(female, 5, WEAR_TYPE.SWIM_TOP, 3000, 3000, 1, false, false);
                    this.setWearShader(female, 2, WEAR_TYPE.BRA, 3100, 2900, 1, true, false);
                    this.setWearShader(female, 3, WEAR_TYPE.SHORTS, 3100, 2900, 1, true, false);
                    this.setWearShader(female, 7, WEAR_TYPE.GLOVE, 3000, 3000, 1, false, false);
                    this.setWearShader(female, 8, WEAR_TYPE.PANST, 3000, 3000, 1, false, false);
                    this.setWearShader(female, 1, WEAR_TYPE.BOTTOM, 3000, 3000, 1, false, false);
                    this.setWearShader(female, 0, WEAR_TYPE.TOP, 3000, 3000, 1, false, true);
                }
            }
			foreach (Male male in Resources.FindObjectsOfTypeAll(typeof(Male)))
			{
				if (male.isActiveAndEnabled)
				{
					this.setAccsShader(false, male);
                    this.setWearShader(male, 0, WEAR_TYPE.TOP, 3000, 3000, 1, false, true);
                    this.setWearShader(male, 10, WEAR_TYPE.SHOES, 3000, 3000, 1, false, false);
                }
            }
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00003210 File Offset: 0x00001410
		public void setItemShader(OCIItem item)
		{
			try
			{
                if (Singleton<Info>.Instance.dicItemLoadInfo.ContainsKey(item.itemInfo.no))
				{
					Info.ItemLoadInfo itemLoadInfo = Singleton<Info>.Instance.dicItemLoadInfo[item.itemInfo.no];
                    this.setItemShader(item.objectItem, itemLoadInfo.bundlePath);
					if (item.isColor2 || item.isChangeColor)
					{
						item.UpdateColor();
					}
				}
			}
			catch (Exception ex)
			{
				this.logSave(ex.ToString());
			}
		}

        public void setItemShader(GameObject obj, string fileName)
        {
            new List<string>();
            Renderer[] renderers_in_children = obj.GetComponentsInChildren<Renderer>(true);
            Projector[] projectors_in_childern = obj.GetComponentsInChildren<Projector>(true);
            foreach(Projector p in projectors_in_childern)
            {
                // test
                p.material.shader = this.presets["Particle Add"].shader;
            }
            foreach(Renderer r in renderers_in_children)
            {
                Type renderertype = r.GetType();
                if (renderertype == typeof(ParticleSystemRenderer) ||
                    renderertype == typeof(LineRenderer) ||
                    renderertype == typeof(TrailRenderer) ||
                    renderertype == typeof(ParticleRenderer) )
                {
                    this.logSave(r.name + " is probably an effects renderer, needs special guesses.");
                    Material particle_mat = r.materials[0]; // assume one particle renderer only uses 1 material.
                    this.logSave("particles!");
                    this.logSave("material:" + fileName + "|" + particle_mat.name);

                    string shader_name = "";
                    string inspector_key = fileName + "|" + particle_mat.name.Replace(" (Instance)", "");

                    if (this.inspector.ContainsKey(inspector_key))
                    {
                        shader_name = this.inspector[inspector_key];
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
                        if (this.presets.ContainsKey(shader_name))
                        {
                            particle_mat.shader = this.presets[shader_name].shader;
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
                                particle_mat.shader = this.presets["Particle Add"].shader;
                            }
                            else if (is_probably_add || shader_name.Contains("Add") || shader_name.Contains("add"))
                            {
                                particle_mat.shader = this.presets["Particle Add"].shader;
                            }
                            else if (is_probably_blend || shader_name.Contains("Blend") || shader_name.Contains("blend"))
                            {
                                particle_mat.shader = this.presets["Particle Alpha Blend"].shader;
                            }
                            else if (is_probably_opaque || shader_name.Contains("Cutout") || shader_name.Contains("Diffuse"))
                            {
                                particle_mat.shader = this.presets["standard"].shader;
                            }
                            else
                            {
                                particle_mat.shader = this.presets["Particle Add"].shader; // catch-all for particles
                            }
                        }
                    }
                    else
                    {
                        this.logSave("Inspector failed to resolve the particle shader name from HS. Which is entirely normal -- however we are going to try guessing what it should map to.");
                        particle_mat.shader = this.presets["Particle Add"].shader; // catch-all for particles
                    }
                    this.logSave("shader:" + particle_mat.shader.name);
                    this.logSave("-- end of one material processing --");
                }
                else
                {
                    foreach (Material material in r.materials)
                    {
                        string shader_name = "";
                        int guessing_renderqueue = -1;
                        if ("".Equals(material.shader.name))
                        {
                            this.logSave("item!");
                            this.logSave("material:" + fileName + "|" + material.name);
                            string inspector_key = fileName + "|" + material.name.Replace(" (Instance)", "");

                            if (this.inspector.ContainsKey(inspector_key))
                            {
                                shader_name = this.inspector[inspector_key];
                                if (shader_name.Length == 0)
                                {
                                    this.logSave("HoneyPotInspector.txt have record of this material, but failed to read its original shader name, likely the shader used by this prefab was not present in the assetbundle. ");
                                }
                            }
                            else
                            {
                                this.logSave("HoneyPotInspector.txt have no record of this material. Resort to default (usually means you have to regenerate HoneyPotInspector.txt)");
                            }

                            if (shader_name.Length == 0)
                            {
                                this.logSave("Inspector failed to resolve the original shader name from HS. Testing a few shader keywords to salvage.");
                                foreach (string text2 in material.shaderKeywords)
                                {
                                    this.logSave("shader keywords found:" + text2);
                                    if (text2.Contains("ALPHAPRE") && !(text2.Contains("LEAF") || text2.Contains("FROND") || text2.Contains("BRANCH")))
                                    {
                                        this.logSave("Possible transparent glasses-like material.");
                                        shader_name = "accessory/ca_megane_hs00|p_asc_glasses_11_00";
                                        guessing_renderqueue = 2501;
                                    }
                                    else if (text2.Contains("TRANS") || text2.Contains("BLEND") || text2.Contains("Blend"))
                                    {
                                        this.logSave("Possible unspecified transparent material.");
                                        shader_name = "PBRsp_alpha";
                                    }
                                    else if (text2.Contains("ALPHATEST") || text2.Contains("LEAF") || text2.Contains("FROND") || text2.Contains("BRANCH"))
                                    {
                                        this.logSave("Possible plant / tree / leaf / branch -like materials.");
                                        shader_name = "_"; // I actually have no idea what is this. Fix later
                                    }
                                }
                            }

                            this.logSave("shader_name:" + shader_name);
                            if (this.presets.ContainsKey(shader_name))
                            {
                                material.shader = this.presets[shader_name].shader;
                            }
                            else
                            {
                                if (shader_name.Contains("Distortion"))
                                {
                                    this.logSave("We should try to import a Distorion effect to PH to deal with this. Right now let's just use simple particle effect shader.");
                                    material.shader = this.presets["Particle Add"].shader;
                                }
                                else
                                {
                                    this.logSave("The preset shaders weren't prepared for this specific HS shader. Likely it was a custom shader, or (less likely) we didn't explore PH shaders enough to find the substitute. Resort to default.");
                                    material.shader = this.presets["standard"].shader;
                                }
                            }
                            if (guessing_renderqueue > 0)
                            {
                                material.renderQueue = guessing_renderqueue;
                            }
                            this.logSave("shader:" + material.shader.name);
                            this.logSave("-- end of one material processing --");
                        }
                    }
                }
            }
        }

        // Token: 0x06000035 RID: 53 RVA: 0x000037CC File Offset: 0x000019CC
        public int getShaderIdx(int wearID, bool doChange)
		{
			int num = this.SHADER_NORMAL_1;
			string @string = ModPrefs.GetString("HoneyPot", wearID.ToString(), this.SHADER_NORMAL_1.ToString(), false);
			try
			{
				num = int.Parse(@string);
			}
			catch (Exception)
			{
			}
			if (doChange)
			{
				num++;
				if (num > this.SHADER_NORMAL_MAX)
				{
					num = this.SHADER_NORMAL_1;
				}
				ModPrefs.SetString("HoneyPot", wearID.ToString(), num.ToString());
			}
			return num;
		}

		// Token: 0x06000036 RID: 54
		public void setAccsShader(bool doStep, Human h)
		{
			try
			{
				Accessories accessories = h.accessories;
				Type type = Assembly.GetAssembly(typeof(Accessories)).GetType("Accessories+AcceObj");
				object value = typeof(Accessories).GetField("acceObjs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(accessories);
				FieldInfo field = type.GetField("obj", BindingFlags.Instance | BindingFlags.Public);
				MethodInfo method = type.GetMethod("SetupMaterials", new Type[]
				{
					typeof(AccessoryData)
				});
				Assembly.GetAssembly(typeof(MaterialCustoms)).GetType("MaterialCustoms+Data_Base").GetField("materials", BindingFlags.Instance | BindingFlags.NonPublic);
				MethodInfo method2 = Assembly.GetAssembly(typeof(MaterialCustoms)).GetType("MaterialCustoms").GetMethod("Setup", new Type[0]);
				int num = -1;
				foreach (object obj in ((Array)value))
				{
					num++;
					AccessoryData accessoryData = accessories.GetAccessoryData(h.customParam.acce, num);
					if (accessoryData == null)
					{
                        continue;
                    }
					try
					{
						GameObject gameObject = (GameObject)field.GetValue(obj);
						Renderer[] renderers_in_acceobj = gameObject.GetComponentsInChildren<Renderer>(true);
						if (gameObject.GetComponent<Destroyer>() == null)
						{
							gameObject.AddComponent<Destroyer>();
						}
						MaterialCustoms materialCustoms = gameObject.AddComponent<MaterialCustoms>();
						materialCustoms.parameters = new MaterialCustoms.Parameter[this.mc.parameters.Length];
						List<string> list = new List<string>();
						foreach (Renderer renderer in renderers_in_acceobj)
						{
							foreach (Material material in renderer.materials)
							{
								if (material.renderQueue <= 3000)
								{
									list.Add(material.name.Replace(" (Instance)", ""));
								}
								if ("".Equals(material.shader.name) || doStep)
								{
									Shader shader;
									if (material.renderQueue <= 3000)
									{
										shader = this.presets[this.presetKeys[this.SHADER_NORMAL_1]].shader;
									}
									else
									{
										shader = this.presets[this.presetKeys[this.SHADER_ACCS_TRANSPARENT]].shader;
									}
									material.shader = shader;
								}
							}
						}
						int num2 = 0;
						foreach (MaterialCustoms.Parameter copy in this.mc.parameters)
						{
							materialCustoms.parameters[num2] = new MaterialCustoms.Parameter(copy);
							materialCustoms.parameters[num2++].materialNames = list.ToArray();
						}
						method2.Invoke(materialCustoms, new object[0]);
						method.Invoke(obj, new object[]
						{
							accessoryData
						});
					}
					catch (Exception ex)
					{
						this.logSave(ex.ToString());
					}
				}
				if (doStep)
				{
					this.searchShaderIdx++;
					if (this.searchShaderIdx >= this.presetKeys.Count)
					{
						this.searchShaderIdx = 0;
					}
				}
			}
			catch (Exception ex2)
			{
				this.logSave(ex2.ToString());
			}
		}

        public void setWearShader(Human h, int idx, WEAR_TYPE type, int maxColorRendererQueue, int maxTransparentRenderQueue, int transparentShaderIdx, bool forceColorable = false, bool isTop = false)
        {
            WearObj wearobj = h.wears.GetWearObj(type);
            if (wearobj == null)
            {
                return;
            }
            try
            {
                Type typeWearObj = Assembly.GetAssembly(typeof(WearObj)).GetType("WearObj");
                WearData wearData = h.wears.GetWearData(type);
                MethodInfo method = typeWearObj.GetMethod("SetupMaterials", new Type[]
                {
                    typeof(WearData)
                });
                MethodInfo method2 = Assembly.GetAssembly(typeof(MaterialCustoms)).GetType("MaterialCustoms").GetMethod("Setup", new Type[0]);
                int wearID = h.customParam.wear.GetWearID(type);
                bool is_a_HS_cloth_parts_that_remmapped_shader = false;
                GameObject gameObject = wearobj.obj;
                Renderer[] renderers_in_wearobj = gameObject.GetComponentsInChildren<Renderer>(true);
                if (gameObject.GetComponent<Destroyer>() == null)
                {
                    gameObject.AddComponent<Destroyer>();
                }
                MaterialCustoms materialCustoms = gameObject.AddComponent<MaterialCustoms>();
                materialCustoms.parameters = new MaterialCustoms.Parameter[this.mc.parameters.Length];
                List<string> list = new List<string>();
                foreach (Renderer renderer in renderers_in_wearobj)
                {
                    foreach (Material material in renderer.materials)
                    {
                        if (((!renderer.name.Contains("_body_") && renderer.tag.Contains("ObjColor")) || forceColorable) &&
                            !material.name.Contains("cf_m_body_CustomMaterial") &&
                            !material.name.Contains("cm_m_body_CustomMaterial") &&
                            "".Equals(material.shader.name))
                        {
                            list.Add(material.name.Replace(" (Instance)", ""));
                        }
                        if ("".Equals(material.shader.name) &&
                            (!renderer.name.Contains("_body_") &&
                            !material.name.Contains("cf_m_body_CustomMaterial") &&
                            !material.name.Contains("cm_m_body_CustomMaterial") &&
                            !renderer.tag.Contains("New tag (8)") || !isTop))
                        {
                            Shader shader = this.getShader(wearID, material.name);
                            material.shader = shader;
                            is_a_HS_cloth_parts_that_remmapped_shader = true;
                        }
                    }
                            
                    GameObject parent_obj = renderer.transform.parent.gameObject;
                    if (type == WEAR_TYPE.SHORTS && (parent_obj.name.Contains("bot_b") || parent_obj.name.Contains("top_a") || parent_obj.name.Contains("top_b")))
                    {
                        parent_obj.SetActive(false);
                    }
                    else if (type == WEAR_TYPE.BRA && (parent_obj.name.Contains("bot_b") || parent_obj.name.Contains("bot_a") || parent_obj.name.Contains("top_b")))
                    {
                        parent_obj.SetActive(false);
                    }
                            
                }
                if (is_a_HS_cloth_parts_that_remmapped_shader)
                {
                    int num2 = 0;
                    foreach (MaterialCustoms.Parameter copy in this.mc.parameters)
                    {
                        materialCustoms.parameters[num2] = new MaterialCustoms.Parameter(copy);
                        materialCustoms.parameters[num2++].materialNames = list.ToArray();
                    }
                    method2.Invoke(materialCustoms, new object[0]);
                    method.Invoke(wearobj, new object[]
                    {
                        wearData
                    });
                    wearobj.UpdateColorCustom();
                    if( this.wearCustomEdit != null && (int)this.nowTabField.GetValue(this.wearCustomEdit) == idx )
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

        // Token: 0x06000037 RID: 55 RVA: 0x0000205B File Offset: 0x0000025B
        public void logSave(string txt)
		{
            Console.WriteLine(txt);
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00003C08 File Offset: 0x00001E08
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

		// Token: 0x06000039 RID: 57 RVA: 0x00003C44 File Offset: 0x00001E44
		public void exportConflict()
		{
			try
			{
				StreamWriter streamWriter = new FileInfo(this.conflictText).CreateText();
				foreach (string value in this.conflictList)
				{
					streamWriter.WriteLine(value);
				}
				streamWriter.Flush();
				streamWriter.Close();
				this.conflictList.Clear();
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00003CD0 File Offset: 0x00001ED0
		public void loadPreset()
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
						if (array.Length >= 5)
						{
							PresetShader presetShader = new PresetShader();
							presetShader.assetBundleName = array[1];
							presetShader.assetName = array[2];
							presetShader.rendererIdx = int.Parse(array[3]);
							presetShader.materialIdx = int.Parse(array[4]);
							presetShader.doCopyMaterial = array[5];
							this.presets.Add(array[0], presetShader);
						}
					}
				}
			}
			catch (Exception ex)
			{
				this.logSave(ex.ToString());
			}
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00003DA8 File Offset: 0x00001FA8
		private void addConflict(int id, string asset1, string asset2, string name1, string name2)
		{
			if (!asset1.Equals(asset2))
			{
				this.conflictList.Add(string.Concat(new object[]
				{
					"[conflict] id:",
					id,
					",asset:",
					name1,
					"(",
					asset1,
					") - ",
					name2,
					"(",
					asset2,
					")"
				}));
			}
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00003E24 File Offset: 0x00002024
		public void getListContent(string assetBundleDir, string fileName)
		{
			Dictionary<int, AccessoryData> dictionary = null;
			Dictionary<int, HairData> dictionary2 = null;
			Dictionary<int, BackHairData> dictionary3 = null;
			Dictionary<int, WearData> dictionary4 = null;
			Dictionary<int, WearData> dictionary5 = null;
			Dictionary<int, WearData> dictionary6 = null;
			Dictionary<int, WearData> dictionary7 = null;
			Dictionary<int, WearData> dictionary8 = null;
			Dictionary<int, WearData> dictionary9 = null;
			Dictionary<int, WearData> dictionary10 = null;
			Dictionary<int, WearData> dictionary11 = null;
			Dictionary<int, WearData> dictionary12 = null;
			Dictionary<int, WearData> dictionary13 = null;
			Dictionary<int, WearData> dictionary14 = null;
			Dictionary<int, PrefabData> dictionary15 = null;
			Dictionary<int, PrefabData> dictionary16 = null;
			Dictionary<int, WearData> dictionary17 = null;
			Dictionary<int, WearData> dictionary18 = null;
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
						dictionary7 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.SWIM);
					}
					else if (textAsset.name.Contains("cf_f_bra"))
					{
						dictionary9 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.BRA);
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
						dictionary12 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.BOTTOM);
					}
					else if (textAsset.name.Contains("cf_f_top"))
					{
						dictionary14 = CustomDataManager.GetWearDictionary_Female(WEAR_TYPE.TOP);
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
					if (dictionary12 != null)
					{
						string[] array10 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int n = 0; n < array10.Length; n++)
						{
							string[] array11 = array10[n].Split(new char[]
							{
								'\t'
							});
							if (array11.Length > 3)
							{
								try
								{
									int num5 = int.Parse(array11[0]) % 1000;
									if (array11[0].Length > 6)
									{
										num5 = int.Parse(array11[0]) % 1000000 + int.Parse(array11[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num5 += 821000;
									}
									WearData wearData3 = new WearData(num5, array11[2], array11[4], array11[6], dictionary12.Count, false);
									wearData3.id = num5;
									if (!dictionary12.ContainsKey(wearData3.id))
									{
										dictionary12.Add(wearData3.id, wearData3);
										HoneyPot.idFileDict[num5] = array11[4];
									}
									else
									{
										this.addConflict(num5, dictionary12[num5].assetbundleName + "/" + dictionary12[num5].prefab, wearData3.assetbundleName + "/" + wearData3.prefab, dictionary12[num5].name, wearData3.name);
									}
								}
								catch (Exception ex5)
								{
									this.logSave(ex5.ToString());
								}
							}
						}
					}
					if (dictionary14 != null)
					{
						string[] array12 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num6 = 0; num6 < array12.Length; num6++)
						{
							string[] array13 = array12[num6].Split(new char[]
							{
								'\t'
							});
							if (array13.Length > 3)
							{
								try
								{
									int num7 = int.Parse(array13[0]) % 1000;
									if (array13[0].Length > 6)
									{
										num7 = int.Parse(array13[0]) % 1000000 + int.Parse(array13[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num7 += 820000;
									}
									WearData wearData4 = new WearData(num7, array13[2], array13[4], array13[6], dictionary14.Count, false);
									wearData4.id = num7;
									if (!dictionary14.ContainsKey(wearData4.id))
									{
										dictionary14.Add(wearData4.id, wearData4);
										HoneyPot.idFileDict[num7] = array13[4];
									}
									else
									{
										this.addConflict(num7, dictionary14[num7].assetbundleName + "/" + dictionary14[num7].prefab, wearData4.assetbundleName + "/" + wearData4.prefab, dictionary14[num7].name, wearData4.name);
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
										dictionary10.Add(wearData7.id, wearData7);
										HoneyPot.idFileDict[num13] = array19[4];
										this.logSave(string.Concat(new object[]
										{
											"idFile",
											num13,
											" ",
											array19[4]
										}));
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
					if (dictionary9 != null)
					{
						string[] array20 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num14 = 0; num14 < array20.Length; num14++)
						{
							string[] array21 = array20[num14].Split(new char[]
							{
								'\t'
							});
							if (array21.Length > 3)
							{
								try
								{
									int num15 = int.Parse(array21[0]) % 1000;
									if (array21[0].Length > 6)
									{
										num15 = int.Parse(array21[0]) % 1000000 + int.Parse(array21[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num15 += 822000;
									}
									WearData wearData8 = new WearData(num15, array21[2], array21[4], array21[6], dictionary9.Count, false);
									wearData8.id = num15;
									wearData8.nip = false;
									WearData wearData9 = dictionary9[1];
									if (!dictionary9.ContainsKey(wearData8.id))
									{
										dictionary9.Add(wearData8.id, wearData8);
										HoneyPot.idFileDict[num15] = array21[4];
									}
									else
									{
										this.addConflict(num15, dictionary9[num15].assetbundleName + "/" + dictionary9[num15].prefab, wearData8.assetbundleName + "/" + wearData8.prefab, dictionary9[num15].name, wearData8.name);
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
					if (dictionary7 != null)
					{
						string[] array24 = textAsset.text.Replace("\r\n", "\n").Split(new char[]
						{
							'\n'
						});
						for (int num18 = 0; num18 < array24.Length; num18++)
						{
							string[] array25 = array24[num18].Split(new char[]
							{
								'\t'
							});
							if (array25.Length > 3)
							{
								try
								{
									int num19 = int.Parse(array25[0]) % 1000;
									if (array25[0].Length > 6)
									{
										num19 = int.Parse(array25[0]) % 1000000 + int.Parse(array25[0].Substring(0, 3)) * 1000;
									}
									else
									{
										num19 += 824000;
									}
									WearData wearData11 = new WearData(num19, array25[2], array25[4], array25[6], dictionary7.Count, false);
									wearData11.id = num19;
									if (!dictionary7.ContainsKey(wearData11.id))
									{
										dictionary7.Add(wearData11.id, wearData11);
										HoneyPot.idFileDict[num19] = array25[4];
									}
									else
									{
										this.addConflict(num19, dictionary7[num19].assetbundleName + "/" + dictionary7[num19].prefab, wearData11.assetbundleName + "/" + wearData11.prefab, dictionary7[num19].name, wearData11.name);
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
									AccessoryData accessoryData = new AccessoryData(num27, array33[2], array33[4], array33[5], array33[5], array33[8], ItemDataBase.SPECIAL.NONE, dictionary.Count, false);
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
					dictionary = null;
					dictionary2 = null;
					dictionary3 = null;
					dictionary4 = null;
					dictionary5 = null;
					dictionary6 = null;
					dictionary7 = null;
					dictionary8 = null;
					dictionary9 = null;
					dictionary10 = null;
					dictionary11 = null;
					dictionary12 = null;
					dictionary13 = null;
					dictionary14 = null;
					dictionary15 = null;
					dictionary16 = null;
					dictionary17 = null;
					dictionary18 = null;
				}
				assetBundle.Unload(true);
			}
			catch (Exception ex19)
			{
				this.logSave(ex19.ToString());
			}
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00005F9C File Offset: 0x0000419C
		private bool doAutoUpdate()
		{
			this.step += 1L;
			int interval = this.getInterval();
			if (interval <= 0 || this.isShaderChanged)
			{
				return false;
			}
			bool flag = this.step % (long)interval == 0L;
			if (flag)
			{
				this.isShaderChanged = true;
			}
			return flag;
		}

		// Token: 0x0600003E RID: 62 RVA: 0x000021EA File Offset: 0x000003EA
		public string getHairPrefabName()
		{
			return ModPrefs.GetString("HoneyPot", "HairPrefabName", "cf_hair_ph00_back", false);
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00002201 File Offset: 0x00000401
		public string getHairAssetBundleName()
		{
			return ModPrefs.GetString("HoneyPot", "HairAssetBundleName", "hair/cf_hair_b_00ph", false);
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00002218 File Offset: 0x00000418
		public string getAccsShaderName()
		{
			return ModPrefs.GetString("HoneyPot", "AccsShaderName", "Legacy Shaders/VertexLit", false);
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00005FE4 File Offset: 0x000041E4
		public int getInterval()
		{
			string @string = ModPrefs.GetString("HoneyPot", "AutoUpdateInterval", "2", false);
			int result = 2;
			try
			{
				result = int.Parse(@string);
			}
			catch (Exception)
			{
			}
			return result;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00006028 File Offset: 0x00004228
		private void Update()
		{
			if (this.isFirst)
			{
				this.loadPreset();
				this.loadOldPreset();
				this.loadDefaultHairShader();
				try
				{
					Dictionary<int, AccessoryData> accessoryDictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.HEAD);
					if (accessoryDictionary.Count == 0)
					{
						return;
					}
					foreach (FileInfo fileInfo in new DirectoryInfo(accessoryDictionary[1].assetbundleDir + "/list/characustom").GetFiles())
					{
						this.getListContent(accessoryDictionary[1].assetbundleDir, "list/characustom/" + fileInfo.Name);
					}
				}
				catch (Exception ex)
				{
					this.logSave(ex.ToString());
				}
				try
				{
					if (Singleton<Studio.Studio>.Instance != null)
					{
						this.readItemResolverText();
						this.readItemResolverTextOld();
					}
				}
				catch (Exception ex2)
				{
					this.logSave(ex2.ToString());
				}
				this.isFirst = false;
				this.readInspector();
				this.exportConflict();
				this.transportDicts();
				this.createCatecory();
            }
			if (Input.GetKeyDown(KeyCode.F12) || this.doAutoUpdate() || HoneyPot.doUpdate)
			{
				HoneyPot.doUpdate = false;
				this.currentFemaleList = (Resources.FindObjectsOfTypeAll(typeof(Female)) as Female[]);
                this.wearCustomEdit    = UnityEngine.Object.FindObjectOfType<WearCustomEdit>();
                this.setAccsShaders();
				this.setHairShaders();
				this.setItemShaders();
			}
			if (this.currentFemaleList != null)
			{
				foreach (Female female in this.currentFemaleList)
				{
					if (female.isActiveAndEnabled)
					{
						if (HoneyPot.idFileDict.ContainsKey(female.customParam.head.eyeBrowID) && this.presets.ContainsKey("PBRsp_texture_alpha"))
						{
							female.head.Rend_eyebrow.material.shader = this.presets["PBRsp_texture_alpha"].shader;
						}
						if (HoneyPot.idFileDict.ContainsKey(female.customParam.head.eyeLashID) && this.presets.ContainsKey("PBRsp_texture_alpha_culloff"))
						{
							female.head.Rend_eyelash.material.shader = this.presets["PBRsp_texture_alpha_culloff"].shader;
						}
					}
				}
			}
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00006274 File Offset: 0x00004474
		private void readItemResolverText()
		{
			try
			{
				Dictionary<int, AccessoryData> accessoryDictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.HEAD);
				if (accessoryDictionary.Count != 0)
				{
					foreach (FileInfo fileInfo in new DirectoryInfo(accessoryDictionary[1].assetbundleDir + "/studioneo/HoneyselectItemResolver").GetFiles())
					{
						this.readItemResolverText(accessoryDictionary[1].assetbundleDir, "studioneo/HoneyselectItemResolver/" + fileInfo.Name);
					}
				}
			}
			catch (Exception ex)
			{
				this.logSave(ex.ToString());
			}
		}

		// Token: 0x06000044 RID: 68 RVA: 0x0000630C File Offset: 0x0000450C
		private void readItemResolverTextOld()
		{
			try
			{
				Dictionary<int, AccessoryData> accessoryDictionary = CustomDataManager.GetAccessoryDictionary(ACCESSORY_TYPE.HEAD);
				if (accessoryDictionary.Count != 0)
				{
					foreach (FileInfo fileInfo in new DirectoryInfo(accessoryDictionary[1].assetbundleDir + "/studio/itemobj/honey/HoneyselectItemResolver").GetFiles())
					{
						this.readItemResolverTextOld(accessoryDictionary[1].assetbundleDir, "studio/itemobj/honey/HoneyselectItemResolver/" + fileInfo.Name);
					}
				}
			}
			catch (Exception ex)
			{
				this.logSave(ex.ToString());
			}
		}

		// Token: 0x06000045 RID: 69 RVA: 0x000063A4 File Offset: 0x000045A4
		public void createCatecory()
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

		// Token: 0x06000046 RID: 70 RVA: 0x0000666C File Offset: 0x0000486C
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
								itemLoadInfo.group = this.toNewCategoly(int.Parse(array[1]));
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

		// Token: 0x06000047 RID: 71 RVA: 0x00006830 File Offset: 0x00004A30
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
								itemLoadInfo.group = this.oldCategolyToNewCategolyAdvance(int.Parse(array[3]));
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

		// Token: 0x06000048 RID: 72 RVA: 0x0000205B File Offset: 0x0000025B
		public void LateUpdate()
		{
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00006970 File Offset: 0x00004B70
		public int toNewCategoly(int cat)
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

		// Token: 0x0600004A RID: 74 RVA: 0x00006A10 File Offset: 0x00004C10
		public int oldCategolyToNewCategolyAdvance(int old)
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

        // Token: 0x04000007 RID: 7
        public static bool doUpdate = false;

		// Token: 0x04000008 RID: 8
		private long step;

		// Token: 0x04000009 RID: 9
		private bool isFirst = true;

		// Token: 0x0400000A RID: 10
		protected Shader orgShader;

		// Token: 0x0400000B RID: 11
		protected MaterialCustoms mc;

        // Token: 0x0400000C RID: 12
        protected Material[] mats;

        // Token: 0x0400000D RID: 13
        protected Material matGlass;

		// Token: 0x0400000E RID: 14
		private Type typeHairs = typeof(Hairs);

		// Token: 0x0400000F RID: 15
		private FieldInfo hairObjField;

		// Token: 0x04000010 RID: 16
		private Assembly libAssembly;

		// Token: 0x04000011 RID: 17
		private Type typeHairObj;

		// Token: 0x04000012 RID: 18
		private FieldInfo gameObjField;

		// Token: 0x04000013 RID: 19
		private Type typeWearCustomEdit = typeof(WearCustomEdit);

        private WearCustomEdit wearCustomEdit = null;

		// Token: 0x04000014 RID: 20
		private FieldInfo nowTabField;

		// Token: 0x04000015 RID: 21
		private bool isShaderChanged;

		// Token: 0x04000016 RID: 22
		private string assetBundlePath = Application.dataPath + "/../abdata";

		// Token: 0x04000017 RID: 23
		private string conflictText = Application.dataPath + "/../UserData/conflict.txt";

		// Token: 0x04000018 RID: 24
		private string inspectorText = Application.dataPath + "/../HoneyPot/HoneyPotInspector.txt";

		// Token: 0x04000019 RID: 25
		private string shaderText = Application.dataPath + "/../HoneyPot/Shader.txt";

		// Token: 0x0400001A RID: 26
		private Dictionary<string, string> inspector = new Dictionary<string, string>();

		// Token: 0x0400001B RID: 27
		private Dictionary<string, PresetShader> presets = new Dictionary<string, PresetShader>();

		// Token: 0x0400001C RID: 28
		private Dictionary<string, PresetShader> oldPresets = new Dictionary<string, PresetShader>();

		// Token: 0x0400001D RID: 29
		private List<string> presetKeys = new List<string>();

		// Token: 0x0400001E RID: 30
		private Dictionary<int, Setting> settings = new Dictionary<int, Setting>();

		// Token: 0x0400001F RID: 31
		private static Dictionary<int, string> idFileDict = new Dictionary<int, string>();

		// Token: 0x04000020 RID: 32
		private List<string> conflictList = new List<string>();

		// Token: 0x04000021 RID: 33
		private int searchShaderIdx;

		// Token: 0x04000022 RID: 34
		private int SHADER_ACCS_TRANSPARENT;

		// Token: 0x04000023 RID: 35
		private int SHADER_NORMAL_1 = 2;

		// Token: 0x04000024 RID: 36
		private int SHADER_NORMAL_MAX = 4;

		// Token: 0x04000025 RID: 37
		private int SHADER_WEAR_TRANSPARENT = 2;

		// Token: 0x04000026 RID: 38
		private static Dictionary<string, int> material_rq = new Dictionary<string, int>();

		// Token: 0x04000027 RID: 39
		private Female[] currentFemaleList = null;
	}
}
