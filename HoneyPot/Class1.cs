using System;
using System.Reflection;
using HarmonyLib;
using IllusionPlugin;
using UnityEngine;

namespace ClassLibrary4
{
	// Token: 0x02000003 RID: 3
	public class Class1 : IEnhancedPlugin, IPlugin
	{
		// Token: 0x06000004 RID: 4 RVA: 0x0000233C File Offset: 0x0000053C
		public void OnApplicationStart()
		{
			if (GameObject.Find("HoneyPot") != null)
			{
				return;
			}
			GameObject gameObject = new GameObject("HoneyPot");
			this.hp = gameObject.AddComponent<HoneyPot>();
			this.hp.gameObject.SetActive(true);
			try
			{
                Harmony harmony = new Harmony(this.Name);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
			catch (Exception ex)
			{
				this.logSave(ex.ToString());
			}
		}

		// Token: 0x06000005 RID: 5 RVA: 0x0000205B File Offset: 0x0000025B
		public void logSave(string txt)
		{
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000006 RID: 6 RVA: 0x0000205D File Offset: 0x0000025D
		public string[] Filter
		{
			get
			{
				return new string[]
				{
					"PlayHomeStudio32bit",
					"PlayHomeStudio64bit",
					"HoneyStudio",
					"PlayClubStudio",
					"PlayClub",
					"PlayHome64bit",
					"PlayHome32bit"
				};
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000007 RID: 7 RVA: 0x0000209D File Offset: 0x0000029D
		public string Name
		{
			get
			{
				return "HoneyPot";
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000008 RID: 8 RVA: 0x000020A4 File Offset: 0x000002A4
		public string Version
		{
			get
			{
				return "1.4.1.2";
			}
		}

		// Token: 0x06000009 RID: 9 RVA: 0x0000205B File Offset: 0x0000025B
		public void OnApplicationQuit()
		{
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000205B File Offset: 0x0000025B
		public void OnFixedUpdate()
		{
		}

		// Token: 0x0600000B RID: 11 RVA: 0x0000205B File Offset: 0x0000025B
		public void OnLateUpdate()
		{
		}

		// Token: 0x0600000C RID: 12 RVA: 0x000020AB File Offset: 0x000002AB
		public void OnLevelWasInitialized(int level)
		{
			this.hp = new GameObject("HoneyPot").AddComponent<HoneyPot>();
			this.hp.gameObject.SetActive(true);
		}

		// Token: 0x0600000D RID: 13 RVA: 0x0000205B File Offset: 0x0000025B
		public void OnLevelWasLoaded(int level)
		{
		}

		// Token: 0x0600000E RID: 14 RVA: 0x0000205B File Offset: 0x0000025B
		public void OnUpdate()
		{
		}

		// Token: 0x04000001 RID: 1
		private HoneyPot hp;
	}
}
