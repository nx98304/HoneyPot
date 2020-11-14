using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace ClassLibrary4
{
	// Token: 0x02000009 RID: 9
	[HarmonyPatch(typeof(ItemFKCtrl), "InitBone", new Type[]
	{
		typeof(OCIItem),
		typeof(Info.ItemLoadInfo),
		typeof(bool)
	})]
	public class ItemFKCtrl_InitBone_Patches
	{
		// Token: 0x06000050 RID: 80 RVA: 0x00002050 File Offset: 0x00000250
		public static bool Prepare()
		{
			return true;
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00006D0C File Offset: 0x00004F0C
		public static bool Prefix(ItemFKCtrl __instance, OCIItem _ociItem, Info.ItemLoadInfo _loadInfo, bool _isNew)
		{
			bool result;
			try
			{
				if (_loadInfo != null && _loadInfo.bones.Count > 0)
				{
					result = true;
				}
				else
				{
					Transform transform = _ociItem.objectItem.transform;
					HashSet<Transform> hashSet = new HashSet<Transform>();
					foreach (Renderer renderer in transform.GetComponentsInChildren<Renderer>(true))
					{
						SkinnedMeshRenderer skinnedMeshRenderer;
						if ((skinnedMeshRenderer = (renderer as SkinnedMeshRenderer)) != null)
						{
							foreach (Transform transform2 in skinnedMeshRenderer.bones)
							{
								if (!(transform2 == null) && !hashSet.Contains(transform2) && !(transform2 == transform))
								{
									hashSet.Add(transform2);
								}
							}
						}
						else if (renderer is MeshRenderer && !hashSet.Contains(renderer.transform))
						{
							if (!renderer.name.Substring(0, renderer.name.Length - 1).EndsWith("MeshPart"))
							{
								if (renderer.transform != transform)
								{
									hashSet.Add(renderer.transform);
								}
							}
							else if (!hashSet.Contains(renderer.transform.parent) && renderer.transform.parent != transform)
							{
								hashSet.Add(renderer.transform.parent);
							}
						}
					}
					_ociItem.listBones = new List<OCIChar.BoneInfo>();
					IList list = (IList)__instance.GetPrivate("listBones");
					ConstructorInfo constructor = list.GetType().GetGenericArguments()[0].GetConstructor(new Type[]
					{
						typeof(GameObject),
						typeof(ChangeAmount),
						typeof(bool)
					});
					int num = 0;
					foreach (Transform transform3 in hashSet)
					{
						OIBoneInfo oiboneInfo = null;
						string pathFrom = transform3.GetPathFrom(transform);
						if (!_ociItem.itemInfo.bones.TryGetValue(pathFrom, out oiboneInfo))
						{
							oiboneInfo = new OIBoneInfo(Studio.Studio.GetNewIndex())
							{
								changeAmount = 
								{
									pos = transform3.localPosition,
									rot = transform3.localEulerAngles,
									scale = transform3.localScale
								}
							};
							_ociItem.itemInfo.bones.Add(pathFrom, oiboneInfo);
						}
						GuideObject guideObject = Singleton<GuideObjectManager>.Instance.Add(transform3, oiboneInfo.dicKey);
						guideObject.enablePos = false;
						guideObject.enableScale = false;
						guideObject.enableMaluti = false;
						guideObject.calcScale = false;
						guideObject.scaleRate = 0.5f;
						guideObject.scaleRot = 0.025f;
						guideObject.scaleSelect = 0.05f;
						guideObject.parentGuide = _ociItem.guideObject;
						_ociItem.listBones.Add(new OCIChar.BoneInfo(guideObject, oiboneInfo));
						guideObject.SetActive(false, true);
						object value = constructor.Invoke(new object[]
						{
							transform3.gameObject,
							oiboneInfo.changeAmount,
							_isNew
						});
						list.Add(value);
						num++;
					}
					__instance.GetType().GetProperty("count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).SetValue(__instance, num, null);
					if (_isNew)
					{
						__instance.ExecuteDelayed(delegate
						{
							_ociItem.ActiveFK(false);
						}, 1);
					}
					else
					{
						__instance.ExecuteDelayed(delegate
						{
							_ociItem.ActiveFK(_ociItem.itemFKCtrl.enabled);
						}, 1);
					}
					result = false;
				}
			}
			catch (Exception)
			{
				result = false;
			}
			return result;
		}

		// Token: 0x06000052 RID: 82 RVA: 0x000070F0 File Offset: 0x000052F0
		public static void Postfix(ItemFKCtrl __instance, OCIItem _ociItem, Info.ItemLoadInfo _loadInfo, bool _isNew)
		{
			try
			{
				IList list = (IList)__instance.GetPrivate("listBones");
				if (list.Count > 0)
				{
					FieldInfo field = list[0].GetType().GetField("changeAmount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
					MethodInfo method = list[0].GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
					foreach (object obj in list)
					{
						ChangeAmount changeAmount = (ChangeAmount)field.GetValue(obj);
						changeAmount.onChangeRot = (Action)Delegate.CreateDelegate(typeof(Action), obj, method);
						changeAmount.onChangeRot();
					}
				}
			}
			catch (Exception)
			{
			}
		}
	}
}
