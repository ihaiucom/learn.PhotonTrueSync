using UnityEngine;
using UnityEditor;
using System.Collections;
namespace UnityEditor
{

	public class SceneViewEditor : EditorWindow
	{

		[MenuItem ("Window/(CC)  场景视窗设置 &v", false, 0)]
		static void Init () {
			SceneViewEditor window = EditorWindow.GetWindow <SceneViewEditor>("场景视窗设置");
			window.Show();
		}


		Transform myt;
		Camera myc;

		SceneView sv;
		Camera sc;
		Transform sct;

		Camera mc;
		Transform msct;
		void SetInit()
		{
//			myt = GameObject.Find("MySceneViewCamera").transform;
//			myc = myt.GetComponent<Camera>();;

			sv = SceneView.lastActiveSceneView;
			if(sv != null)
			{
				sc = SceneView.lastActiveSceneView.camera;
				sct = sc.gameObject.transform;
			}

			
			mc = Camera.main;
			msct = mc.gameObject.transform;
		}

		void OnGUI()
		{
			SetInit();

			if (GUILayout.Button("主摄像机->场景视窗"))
			{
				sv.pivot = msct.position;
				sv.rotation = msct.rotation;
				sv.size = mc.orthographicSize * 2;
				sv.orthographic = mc.orthographic;

				sc.fieldOfView = mc.fieldOfView;
			}

			GUILayout.BeginVertical();

			//-----------------------
			GUILayout.Box("Scene View Camera", GUILayout.ExpandWidth(true));
			GUILayout.BeginHorizontal();
			GUILayout.Label("Aspect", GUILayout.Width(100));
			sc.aspect = GUILayout.HorizontalSlider(sc.aspect, 0, 100);
			GUILayout.Space(10);
			EditorGUILayout.FloatField(sc.aspect , GUILayout.Width(80));
			GUILayout.EndHorizontal();

			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Field Of View", GUILayout.Width(100));
			sc.fieldOfView = GUILayout.HorizontalSlider(sc.fieldOfView, 0, 100);
			GUILayout.Space(10);
			EditorGUILayout.FloatField(sc.fieldOfView , GUILayout.Width(80));
			GUILayout.EndHorizontal();

			
			sct.position = EditorGUILayout.Vector3Field("Position", sct.position);
			sct.rotation =Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", sct.rotation.eulerAngles));

			//----------------------
			GUILayout.Space(10);
			GUILayout.Box("Scene View", GUILayout.ExpandWidth(true));

			sv.pivot = EditorGUILayout.Vector3Field("Pivot", sv.pivot);

			sv.rotation =Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", sv.rotation.eulerAngles));
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Size", GUILayout.Width(100));
			sv.size = GUILayout.HorizontalSlider(sv.size, 0, 40000);
			GUILayout.Space(10);
			sv.size = EditorGUILayout.FloatField(sv.size, GUILayout.Width(80));
			GUILayout.EndHorizontal();

			sv.orthographic = EditorGUILayout.Toggle("Orthographic", sv.orthographic);
			sv.in2DMode = EditorGUILayout.Toggle("2D", sv.in2DMode);

			
			//----------------------
			GUILayout.Space(10);
			GUILayout.Box("快捷方式", GUILayout.ExpandWidth(true));
			GUILayout.Label(@"应用/保存 快捷方式，请看Edit/(CC)  视窗姿态

Edit/(CC)  主摄像机->场景视窗 		Alt + `
Edit/(CC)  场景视窗->主摄像机 		Shit + `

Edit/(CC)  视窗姿态1    			Alt + 1
Edit/(CC)  视窗姿态2    			Alt + 2
Edit/(CC)  视窗姿态3    			Alt + 3
Edit/(CC)  视窗姿态4    			Alt + 4
Edit/(CC)  视窗姿态5    			Alt + 5

Edit/(CC)  保存--视窗姿态1 		Shift + 1
Edit/(CC)  保存--视窗姿态2 		Shift + 2
Edit/(CC)  保存--视窗姿态3 		Shift + 3
Edit/(CC)  保存--视窗姿态4 		Shift + 4
Edit/(CC)  保存--视窗姿态5 		Shift + 5
");

			GUILayout.EndVertical();
		}
	}
}