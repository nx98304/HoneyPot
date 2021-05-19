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
            try
			{
                harmony = new Harmony(this.Name);
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
            Console.WriteLine(txt);
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
				return "1.4.9";
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
            this.logSave("HoneyPot - OnLevelWasInitialized: Level " + level);
            //if (GameObject.Find("HoneyPot") != null) return;
            //if ( level == 1 )
            //{
            //    this.logSave("HoneyPot confirmed level == 1, and hasn't inited yet. Initializing.");
            //    this.hp = new GameObject("HoneyPot").AddComponent<HoneyPot>();
            //    this.hp.gameObject.SetActive(true);
            //}
		}

		// Token: 0x0600000D RID: 13 RVA: 0x0000205B File Offset: 0x0000025B
		public void OnLevelWasLoaded(int level)
		{
            this.logSave("HoneyPot - OnLevelWasLoaded: Level " + level);
            if (GameObject.Find("HoneyPot") != null) return;

            // Note: The lobby scene, chara maker and H scene all have different level value that is > 0
            //       however, studio startup already is level 0 and without the safe guard HoneyPot gets duplicated.
            //       and the Studio fully loaded scene is level 1
            //       If it is studio, we actually don't want to load HoneyPot too early
            //       to avoid multiple initializations and some erroneous order of init causing NREs
            if (level > 0)
            {
                this.logSave("HoneyPot (re-)initializing after level was loaded.");
                GameObject gameObject = new GameObject("HoneyPot");
                this.hp = gameObject.AddComponent<HoneyPot>();
                this.hp.SetHarmony(harmony);
                this.hp.gameObject.SetActive(true);
            }
        }

		// Token: 0x0600000E RID: 14 RVA: 0x0000205B File Offset: 0x0000025B
		public void OnUpdate()
		{
		}

        // Token: 0x04000001 RID: 1
        private Harmony harmony;
		private HoneyPot hp;
	}
}
