using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;


namespace UnityEditor
{
	public class SceneViewSettingData : ScriptableObject
	{
		public int id = 0;
		
		//SceneView
		public Vector3 		pivot;
		public Quaternion 	rotation;
		public float 		size;
		public bool			orthographic;
		
		//SceneView.Camera
		public float		fieldOfView;
		
		public SceneViewSettingData(int id)
		{
			this.id = id;
		}
		
		
		
		
		/** 保存 */
		public void Save()
		{
			string path =GetPath(id);
			CheckPath(path);
			AssetDatabase.CreateAsset(this, path);
			AssetDatabase.SaveAssets();
			Debug.Log(AssetDatabase.GetAssetPath(this));
		}
		
		/** Data- > SceneView  */
		public void D2S()
		{
			SceneView 	sv = SceneView.lastActiveSceneView;
			Camera 		sc = sv.camera;
			
			sv.pivot 			= pivot;
			sv.rotation 		= rotation;
			sv.size 			= size;
			sv.orthographic 	= orthographic;
			sc.fieldOfView 		= fieldOfView;
		}
		
		/**  SceneView - > Data */
		public void S2D()
		{
			SceneView 	sv = SceneView.lastActiveSceneView;
			Camera 		sc = sv.camera;
			
			pivot			= sv.pivot;
			rotation		= sv.rotation;
			size			= sv.size;
			orthographic	= sv.orthographic;
			fieldOfView		= sc.fieldOfView;
		}
		
		public static string GetPath(int id)
		{
			return  "Assets/_Temp/SceneViewSettingData_" + id + ".asset";
		}
		
		public static SceneViewSettingData Read(int id)
		{
			string path =GetPath(id);
			SceneViewSettingData data = AssetDatabase.LoadAssetAtPath<SceneViewSettingData>(path);
			if(data != null)
			{
				data.D2S();
			}
			else
			{
				Debug.Log("还没设置'视窗姿态"+ id + "'");
			}
			
			return data;
		}
		
		
		public static SceneViewSettingData Write(int id)
		{
			SceneViewSettingData data = new SceneViewSettingData(id);
			data.S2D();
			data.Save();
			return data;
		}

		
		public static void CheckPath(string path, bool isFile = true)
		{
			if(isFile) path = path.Substring(0, path.LastIndexOf('/'));
			string[] dirs = path.Split('/');
			string target = "";
			
			bool first = true;
			foreach(string dir in dirs)
			{
				if(first)
				{
					first = false;
					target += dir;
					continue;
				}
				
				if(string.IsNullOrEmpty(dir)) continue;
				target +="/"+ dir;
				if(!Directory.Exists(target))
				{
					Directory.CreateDirectory(target);
				}
			}
		}

		//---------------------------

		[MenuItem ("Edit/(CC)  主摄像机->场景视窗 &`", false, 900)]
		public static void MainCamera2SceneView()
		{
			SceneView 	sv = SceneView.lastActiveSceneView;
			Camera 		sc = sv.camera;
			Transform 	sct = sv.camera.transform;
			
			Camera 		mc = Camera.main;
			Transform 	msct = mc.gameObject.transform;
			
			sv.pivot 			= msct.position;
			sv.rotation 		= msct.rotation;
			sv.size 			= mc.orthographicSize * 2;
			sv.orthographic 	= mc.orthographic;
			sc.fieldOfView 		= mc.fieldOfView;

//			sct.position		= msct.position;
//			sct.rotation		= msct.rotation;
		}

		
		[MenuItem ("Edit/(CC)  场景视窗->主摄像机 #`", false, 900)]
		public static void SceneView2MainCamera()
		{
			SceneView 	sv = SceneView.lastActiveSceneView;
			Camera 		sc = sv.camera;
			Transform 	sct = sv.camera.transform;
			
			Camera 		mc = Camera.main;
			Transform 	msct = mc.gameObject.transform;
			
			msct.position 			= sct.position;
			msct.rotation 			= sct.rotation;
			mc.orthographicSize 	= sv.size * 0.5f;
			mc.orthographic 		= sv.orthographic ;
			mc.fieldOfView 			= sc.fieldOfView;
		}
		
		
		//-------------------------
		[MenuItem ("Edit/(CC)  视窗姿态1 &1", false, 900)]
		public static void ReadSceneVale_1()
		{
			SceneViewSettingData.Read(1);
		}
		
		[MenuItem ("Edit/(CC)  视窗姿态2 &2", false, 900)]
		public static void ReadSceneVale_2()
		{
			SceneViewSettingData.Read(2);
		}
		
		
		[MenuItem ("Edit/(CC)  视窗姿态3 &3", false, 900)]
		public static void ReadSceneVale_3()
		{
			SceneViewSettingData.Read(3);
		}
		
		[MenuItem ("Edit/(CC)  视窗姿态4 &4", false, 900)]
		public static void ReadSceneVale_4()
		{
			SceneViewSettingData.Read(4);
		}
		
		[MenuItem ("Edit/(CC)  视窗姿态5 &5", false, 900)]
		public static void ReadSceneVale_5()
		{
			SceneViewSettingData.Read(5);
		}
		
		//----------------------
		
		[MenuItem ("Edit/(CC)  保存--视窗姿态1 #1", false, 900)]
		public static void SaveSceneVale_1()
		{
			SceneViewSettingData.Write(1);
		}
		
		[MenuItem ("Edit/(CC)  保存--视窗姿态2 #2", false, 900)]
		public static void SaveSceneVale_2()
		{
			SceneViewSettingData.Write(2);
		}
		
		[MenuItem ("Edit/(CC)  保存--视窗姿态3 #3", false, 900)]
		public static void SaveSceneVale_3()
		{
			SceneViewSettingData.Write(3);
		}
		
		[MenuItem ("Edit/(CC)  保存--视窗姿态4 #4", false, 900)]
		public static void SaveSceneVale_4()
		{
			SceneViewSettingData.Write(4);
		}
		
		[MenuItem ("Edit/(CC)  保存--视窗姿态5 #5", false, 900)]
		public static void SaveSceneVale_5()
		{
			SceneViewSettingData.Write(5);
		}
		
		//-----------------------------

	}
}