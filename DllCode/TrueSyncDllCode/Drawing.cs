using System;
using UnityEngine;

public class Drawing
{
	public static Texture2D lineTex;

	public static void DrawLine(Rect rect)
	{
		Drawing.DrawLine(rect, GUI.get_contentColor(), 1f);
	}

	public static void DrawLine(Rect rect, Color color)
	{
		Drawing.DrawLine(rect, color, 1f);
	}

	public static void DrawLine(Rect rect, float width)
	{
		Drawing.DrawLine(rect, GUI.get_contentColor(), width);
	}

	public static void DrawLine(Rect rect, Color color, float width)
	{
		Drawing.DrawLine(new Vector2(rect.get_x(), rect.get_y()), new Vector2(rect.get_x() + rect.get_width(), rect.get_y() + rect.get_height()), color, width);
	}

	public static void DrawLine(Vector2 pointA, Vector2 pointB)
	{
		Drawing.DrawLine(pointA, pointB, GUI.get_contentColor(), 1f);
	}

	public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color)
	{
		Drawing.DrawLine(pointA, pointB, color, 1f);
	}

	public static void DrawLine(Vector2 pointA, Vector2 pointB, float width)
	{
		Drawing.DrawLine(pointA, pointB, GUI.get_contentColor(), width);
	}

	public static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)
	{
		Matrix4x4 matrix = GUI.get_matrix();
		bool flag = !Drawing.lineTex;
		if (flag)
		{
			Drawing.lineTex = new Texture2D(1, 1);
		}
		Color color2 = GUI.get_color();
		GUI.set_color(color);
		float num = Vector3.Angle(pointB - pointA, Vector2.get_right());
		bool flag2 = pointA.y > pointB.y;
		if (flag2)
		{
			num = -num;
		}
		GUIUtility.ScaleAroundPivot(new Vector2((pointB - pointA).get_magnitude(), width), new Vector2(pointA.x, pointA.y + 0.5f));
		GUIUtility.RotateAroundPivot(num, pointA);
		GUI.DrawTexture(new Rect(pointA.x, pointA.y, 1f, 1f), Drawing.lineTex);
		GUI.set_matrix(matrix);
		GUI.set_color(color2);
	}
}
