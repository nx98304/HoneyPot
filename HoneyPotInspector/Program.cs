using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using Microsoft.CSharp.RuntimeBinder;
using UnityPlugin;

namespace HoneyPotInspector
{
	internal class Program
	{
        private static Dictionary<string, string> ret = new Dictionary<string, string>();
        private static string exePath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\";

        private static void Main(string[] args)
        {
            Dictionary<string, long> datetime_dict = new Dictionary<string, long>();
            try
            {
                StreamReader datetime_reader = new StreamReader(exePath + "../HoneyPot/FileDateTime.txt", Encoding.GetEncoding("UTF-8"));
                string text2;
                while ((text2 = datetime_reader.ReadLine()) != null)
                {
                    string[] array2 = text2.Split(',');
                    if (array2.Length >= 2)
                    {
                        string key = array2[0];
                        datetime_dict[key] = long.Parse(array2[1]);
                    }
                }
                datetime_reader.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("FileDateTime.txt wasn't found. Don't worry as it will be created later.");
            }
            try
            {
                StreamReader main_reader = new StreamReader(exePath + "../HoneyPot/HoneyPotInspector.txt", Encoding.GetEncoding("UTF-8"));
                string text;
                while ((text = main_reader.ReadLine()) != null)
                {
                    string[] array = text.Split(',');
                    string filepath = array[0].Split('|')[0];
                    int n = array.Length;
                    int dummy = 0;
                    if (n >= 3) // It's only possible to have CRQ data when we have >= 3 Length
                    {
                        StringBuilder str = new StringBuilder();
                        str.Append(array[0]);
                        for (int i = 1; i <= n - 3; i++) //when n >= 4, this for loop will run.
                            str.Append(String.Concat(",", array[i]));
                                                                //n-2 has to be shader_name
                        ret[str.ToString()] = String.Concat(array[n-2], ",", array[n-1]);
                                                                                 //n-1 has to be CRQ
                        // If n-1 isn't an int (so not CRQ value) or shader_name is somehow empty
                        if ( !int.TryParse(array[n-1], out dummy) || array[n-2].Length == 0 )
                        { //Then we still force the reprocessing by resetting FileDateTime.
                            Console.WriteLine("HoneyPotInspector.txt: " + filepath + " recorded in old format. Resetting its FileDateTime timestamp to force reprocessing.");
                            datetime_dict[filepath] = 0;
                        }
                    }                                                                
                    else if(n == 2)
                    {
                        Console.WriteLine("HoneyPotInspector.txt: " + filepath + " recorded in old format. Resetting its FileDateTime timestamp to force reprocessing.");
                        datetime_dict[filepath] = 0;
                        ret[array[0]] = array[1];
                    }
                    else if(n == 1)
                    {
                        Console.WriteLine("HoneyPotInspector.txt: " + filepath + " recorded in old format. Resetting its FileDateTime timestamp to force reprocessing.");
                        datetime_dict[filepath] = 0;
                        ret[array[0]] = "";
                    }
                }
                main_reader.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("HoneyPotInspector.txt wasn't found. Don't worry as it will be created later.");
            }
            string[] filesMostDeep = GetFilesMostDeep(exePath + "../abdata/", "*.unity3d");
            if (filesMostDeep == null)
            {
                return;
            }
            string[] filelist = filesMostDeep;
            for (int i = 0; i < filelist.Length; i++)
            {
                string filepath = filelist[i].Substring(filelist[i].IndexOf("abdata") + 7).Replace("\\", "/");
                DateTime creationTimeUtc = File.GetCreationTimeUtc(filelist[i]);
                DateTime d = new DateTime(1970, 1, 1, 0, 0, 0, creationTimeUtc.Kind);
                long num = Convert.ToInt64(Math.Floor((creationTimeUtc - d).TotalSeconds));
                if (!datetime_dict.ContainsKey(filepath))
                {
                    getShaderNameFromFile(filelist[i]);
                    datetime_dict[filepath] = num;
                }
                else if (datetime_dict[filepath] != num)
                {
                    Console.WriteLine(filepath + " needs updating because the timestamp differs from the record. (file: {0}, new: {1})", datetime_dict[filepath], num);
                    getShaderNameFromFile(filelist[i]);
                    datetime_dict[filepath] = num;
                }
                else
                {
                    Console.WriteLine(filepath + " (time: " + datetime_dict[filepath] + ") is already in the filedatemap and the timestamp is the same.");
                }
            }
            outDict();
            StreamWriter datetime_writer = new StreamWriter(exePath + "../HoneyPot/FileDateTime.txt", false, Encoding.GetEncoding("UTF-8"));
            foreach (KeyValuePair<string, long> item in datetime_dict)
            {
                string value = item.Key + "," + item.Value;
                datetime_writer.WriteLine(value);
            }
            datetime_writer.Close();
        }

        public static string[] GetFilesMostDeep(string stRootPath, string stPattern)
        {
            StringCollection stringCollection = new StringCollection();
            string[] array = null;
            try
            {
                string[] files = Directory.GetFiles(stRootPath, stPattern);
                foreach (string value in files)
                {
                    stringCollection.Add(value);
                }
                string[] directories = Directory.GetDirectories(stRootPath);
                for (int j = 0; j < directories.Length; j++)
                {
                    string[] filesMostDeep = GetFilesMostDeep(directories[j], stPattern);
                    if (filesMostDeep != null)
                    {
                        stringCollection.AddRange(filesMostDeep);
                    }
                }
                array = new string[stringCollection.Count];
                stringCollection.CopyTo(array, 0);
                return array;
            }
            catch (Exception)
            {
                return array;
            }
        }

        public static void getShaderNameFromFile(string fileName)
        {
            try
            {
                Console.WriteLine("Reading: " + fileName);
                UnityParser unityParser = new UnityParser(fileName);
                int count = unityParser.Cabinet.Components.Count;
                string text = fileName.Substring(fileName.IndexOf("abdata") + 7);
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        switch (unityParser.Cabinet.Components[i].classID())
                        {
                            case UnityClassID.MeshRenderer:
                            case UnityClassID.ParticleRenderer:
                            case UnityClassID.TrailRenderer:
                            case UnityClassID.LineRenderer:
                            case UnityClassID.SkinnedMeshRenderer:
                            case UnityClassID.ParticleSystemRenderer:
                                foreach (dynamic item in unityParser.Cabinet.LoadComponent(unityParser.Cabinet.Components[i].pathID).m_Materials)
                                {
                                    Material material = (Material)((PPtr<Material>)item).asset;
                                    Component component = material.m_Shader.asset;
                                    try
                                    {
                                        string shader_name = "";
                                        //Note: Make sure the main Shader discovery process is all within FileID == 0
                                        //      Which means it is in the same abdata and not relying on external files.
                                        //      HPI will never be able to handle manifests & external files properly
                                        //      for HS1 items. 
                                        // if (material.m_Shader.m_FileID == 0)
                                        if (material.m_Shader.m_FileID == 0 && material.m_Shader.m_PathID != 0)
                                        {
                                            //shader_name = ((component is NotLoaded) ? 
                                            //                  ((NotLoaded)component).Name : 
                                            //                  ((component.file.VersionNumber < AssetCabinet.VERSION_5_5_0) ? 
                                            //                      ((Shader)component).m_Name : 
                                            //                      ((Shader)component).m_ParsedForm.m_Name)
                                            //              );
                                            //long _dummy_ = 0;
                                            ////Note: Not only we want to avoid empty shader names, but some modder strangely
                                            ////      renames shaders into random numbers. We don't want those either.
                                            //if ((shader_name.Equals("") || long.TryParse(shader_name, out _dummy_)) &&
                                            //    material.m_Shader.m_PathID != 0)
                                            //{
                                            //    component = unityParser.Cabinet.LoadComponent(material.m_Shader.m_PathID);
                                            //    Shader shader = (Shader)component;
                                            //    if (shader != null)
                                            //    {
                                            //        string shader_script = shader.m_Script;
                                            //        if (shader_script != null && shader_script.Length > 0)
                                            //        {
                                            //            shader_script = shader_script.Substring(shader_script.IndexOf("\"") + 1);
                                            //            shader_name = shader_script.Substring(0, shader_script.IndexOf("\""));
                                            //        }
                                            //    }
                                            //    //Shader.m_Script seem to start with [[Shader "name"...]], so we just grab the string in between
                                            //    //the first quote and the second quote? That easy?
                                            //}
                                            if ( component.file.VersionNumber < AssetCabinet.VERSION_5_5_0 )
                                            {
                                                Shader shader = (Shader)unityParser.Cabinet.LoadComponent(material.m_Shader.m_PathID);
                                                if (shader != null)
                                                {
                                                    string shader_script = shader.m_Script;
                                                    if (shader_script != null && shader_script.Length > 0)
                                                    {
                                                        shader_script = shader_script.Substring(shader_script.IndexOf("\"") + 1);
                                                        shader_name = shader_script.Substring(0, shader_script.IndexOf("\""));
                                                    }
                                                }
                                                //Shader.m_Script seem to start with [[Shader "name"...]], so we just grab the string in between
                                                //the first quote and the second quote. But this seems to be also true for Unity 5.3 shaders.
                                            }
                                            //If trying to get the Unity 5.3 shader.m_Script somehow fails, try the basic m_Name.
                                            if (shader_name.Equals(""))
                                            {
                                                shader_name = ((component is NotLoaded) ?
                                                                  ((NotLoaded)component).Name :
                                                                  ((component.file.VersionNumber < AssetCabinet.VERSION_5_5_0) ?
                                                                      ((Shader)component).m_Name :
                                                                      ((Shader)component).m_ParsedForm.m_Name)
                                                              );
                                            }
                                            //The last resort is still the PathID, as long as the PathID isn't 0.
                                            if (shader_name.Equals(""))
                                            {
                                                shader_name = material.m_Shader.m_PathID.ToString();
                                            }
                                        }
                                        else
                                        {
                                            switch ( material.m_Shader.m_PathID )
                                            {
                                                case                    6: shader_name = "Normal-VertexLit"; break;
                                                case                    7: shader_name = "Normal-Diffuse"; break;
                                                case -9046184444883282072: shader_name = "Shader Forge/PBRsp_texture_alpha_culloff"; break;
                                                default:                   shader_name = "Standard"; break;
                                            }
                                            //Note: For certain objects relying on unity_builtin_extra's shaders,
                                            //      Usually they are just for Diffuse or VertexLit, which are just
                                            //      Standard since Unity 5.
                                            //      There will be other mod packs with expectation of a manifest,
                                            //      for such mods, they could use whatever shaders that HPI will never
                                            //      be able to identify within the scope of the same abdata
                                            //      so that has to be ruled out as impossible for the foreseeable future.
                                        }
                                        Console.WriteLine(text + " - " + material.m_Name + " - " + shader_name + " - " + material.m_CustomRenderQueue);
                                        ret[text.Replace("\\", "/") + "|" + material.m_Name] = shader_name + "," + material.m_CustomRenderQueue;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());
                                        Console.WriteLine(ex.StackTrace);
                                    }
                                }
                                break;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                unityParser.Dispose();
            }
            catch (Exception ex3)
            {
                Console.WriteLine(ex3.ToString());
                Console.WriteLine(ex3.StackTrace);
            }
        }

        public static void outDict()
        {
            StreamWriter streamWriter = new StreamWriter(exePath + "../HoneyPot/HoneyPotInspector.txt", false, Encoding.GetEncoding("UTF-8"));
            foreach (KeyValuePair<string, string> item in ret)
            {
                string value = item.Key + "," + item.Value;
                streamWriter.WriteLine(value);
            }
            streamWriter.Close();
        }
    }
}
