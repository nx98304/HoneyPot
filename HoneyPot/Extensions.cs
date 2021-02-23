using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace ClassLibrary4
{
	// Token: 0x02000005 RID: 5
    public static class StringExtensions
    {
        public static bool Contains_NoCase(this string self, string str)
        {
            return self.IndexOf(str, StringComparison.CurrentCultureIgnoreCase) != -1;
        }
    }

	public static class Extensions
	{
		// Token: 0x06000012 RID: 18 RVA: 0x000020E3 File Offset: 0x000002E3
		public static void SetPrivateExplicit<T>(this T self, string name, object value)
		{
			typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).SetValue(self, value);
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002103 File Offset: 0x00000303
		public static void SetPrivate(this object self, string name, object value)
		{
			self.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).SetValue(self, value);
		}

		// Token: 0x06000014 RID: 20 RVA: 0x0000211A File Offset: 0x0000031A
		public static object GetPrivateExplicit<T>(this T self, string name)
		{
			return typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).GetValue(self);
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002139 File Offset: 0x00000339
		public static object GetPrivate(this object self, string name)
		{
			return self.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).GetValue(self);
		}

		// Token: 0x06000016 RID: 22 RVA: 0x0000214F File Offset: 0x0000034F
		public static object CallPrivateExplicit<T>(this T self, string name, params object[] p)
		{
			return typeof(T).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Invoke(self, p);
		}

		// Token: 0x06000017 RID: 23 RVA: 0x0000216F File Offset: 0x0000036F
		public static object CallPrivate(this object self, string name, params object[] p)
		{
			return self.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Invoke(self, p);
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002186 File Offset: 0x00000386
		public static void ExecuteDelayed(this MonoBehaviour self, Action action, int waitCount = 1)
		{
			self.StartCoroutine(Extensions.ExecuteDelayed_Routine(action, waitCount));
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002196 File Offset: 0x00000396
		private static IEnumerator ExecuteDelayed_Routine(Action action, int waitCount)
		{
			int num;
			for (int i = 0; i < waitCount; i = num)
			{
				yield return null;
				num = i + 1;
			}
			action();
			yield break;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000023BC File Offset: 0x000005BC
		public static string GetPathFrom(this Transform self, Transform root)
		{
			string text = self.name;
			Transform parent = self.parent;
			while (parent != root)
			{
				text = parent.name + "/" + text;
				parent = parent.parent;
			}
			return text;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000023FC File Offset: 0x000005FC
		public static Transform FindDescendant(this Transform self, string name)
		{
			if (self.name.Equals(name))
			{
				return self;
			}
			foreach (object obj in self)
			{
				Transform transform = ((Transform)obj).FindDescendant(name);
				if (transform != null)
				{
					return transform;
				}
			}
			return null;
		}
	}
}
