using System.Collections.Generic;
using UnityEngine;

public class ScaleGUI
{
	public static bool scaleScreen;

	public static float WIDTH;

	public static float HEIGHT;

	private static List<Matrix4x4> stack = new List<Matrix4x4>();
	private static Rect systemBarArea;
	private static float nextInsetsRead;
	private static int insetWidth, insetHeight;
	private static bool insetWarningLogged;

	// Android 15 can draw behind gesture navigation even when fullscreen is disabled.
	// Work in GUI's top-left coordinates; game layout retains its existing logical size.
	public static Rect Viewport
	{
		get
		{
			if (Application.platform != RuntimePlatform.Android)
				return new Rect(0, 0, Screen.width, Screen.height);
			if (Time.realtimeSinceStartup >= nextInsetsRead || insetWidth != Screen.width || insetHeight != Screen.height)
				ReadSystemBarArea();
			Rect safe = Screen.safeArea;
			float left = Mathf.Max(0, Mathf.Max(safe.xMin, systemBarArea.xMin));
			float bottom = Mathf.Max(0, Mathf.Max(safe.yMin, systemBarArea.yMin));
			float right = Mathf.Min(Screen.width, Mathf.Min(safe.xMax, systemBarArea.xMax));
			float top = Mathf.Min(Screen.height, Mathf.Min(safe.yMax, systemBarArea.yMax));
			if (right <= left || top <= bottom) return new Rect(0, 0, Screen.width, Screen.height);
			return new Rect(left, Screen.height - top, right - left, top - bottom);
		}
	}

	private static void ReadSystemBarArea()
	{
		insetWidth = Screen.width;
		insetHeight = Screen.height;
		nextInsetsRead = Time.realtimeSinceStartup + 0.5f;
		systemBarArea = new Rect(0, 0, Screen.width, Screen.height);
		try
		{
			using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
			{
				if (version.GetStatic<int>("SDK_INT") < 30) return;
			}
			using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			using (AndroidJavaObject activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
			using (AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow"))
			using (AndroidJavaObject decor = window.Call<AndroidJavaObject>("getDecorView"))
			{
				// When Unity already has a smaller Player window, safeArea is window-relative.
				// Applying full-window native insets there would reserve the same space twice.
				if (decor.Call<int>("getWidth") != Screen.width || decor.Call<int>("getHeight") != Screen.height) return;
				using (AndroidJavaObject rootInsets = decor.Call<AndroidJavaObject>("getRootWindowInsets"))
				using (AndroidJavaClass type = new AndroidJavaClass("android.view.WindowInsets$Type"))
				{
					if (rootInsets == null) return;
					using (AndroidJavaObject bars = rootInsets.Call<AndroidJavaObject>("getInsets", type.CallStatic<int>("systemBars")))
					{
						int left = bars.Get<int>("left"), right = bars.Get<int>("right");
						int top = bars.Get<int>("top"), bottom = bars.Get<int>("bottom");
						systemBarArea = new Rect(left, bottom, Screen.width - left - right, Screen.height - top - bottom);
					}
				}
			}
		}
		catch (System.Exception ex)
		{
			if (!insetWarningLogged) Debug.LogWarning("System bar insets unavailable; using Screen.safeArea (" + ex.GetType().Name + ").");
			insetWarningLogged = true;
		}
	}

	public static Vector2 MapToLogical(Vector2 point, Rect viewport, float logicalWidth, float logicalHeight)
	{
		return new Vector2((point.x - viewport.x) * logicalWidth / viewport.width,
			(point.y - viewport.y) * logicalHeight / viewport.height);
	}

	public static Vector2 ToGuiPoint(Vector2 point)
	{
		if (Application.platform != RuntimePlatform.Android) return point;
		return MapToLogical(point, Viewport, WIDTH, HEIGHT);
	}

	public static void initScaleGUI()
	{
		Cout.println("Init Scale GUI: Screen.w=" + Screen.width + " Screen.h=" + Screen.height);
		WIDTH = Screen.width;
		HEIGHT = Screen.height;
		scaleScreen = false;
		if (Screen.width <= 1200)
		{
		}
	}

	public static void BeginGUI()
	{
		stack.Add(GUI.matrix);
		if (Application.platform == RuntimePlatform.Android)
		{
			Rect viewport = Viewport;
			Color previousColor = GUI.color;
			GUI.color = Color.black;
			GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
			GUI.color = previousColor;
			GUI.matrix *= Matrix4x4.TRS(new Vector3(viewport.x, viewport.y, 0), Quaternion.identity,
				new Vector3(viewport.width / WIDTH, viewport.height / HEIGHT, 1));
			return;
		}
		if (scaleScreen)
		{
			Matrix4x4 matrix4x = default(Matrix4x4);
			float num = Screen.width;
			float num2 = Screen.height;
			float num3 = num / num2;
			float num4 = 1f;
			Vector3 zero = Vector3.zero;
			num4 = ((!(num3 < WIDTH / HEIGHT)) ? ((float)Screen.height / HEIGHT) : ((float)Screen.width / WIDTH));
			matrix4x.SetTRS(zero, Quaternion.identity, Vector3.one * num4);
			GUI.matrix *= matrix4x;
		}
	}

	public static void EndGUI()
	{
		if (stack.Count > 0)
		{
			GUI.matrix = stack[stack.Count - 1];
			stack.RemoveAt(stack.Count - 1);
		}
	}

	public static float scaleX(float x)
	{
		if (!scaleScreen)
		{
			return x;
		}
		x = x * WIDTH / (float)Screen.width;
		return x;
	}

	public static float scaleY(float y)
	{
		if (!scaleScreen)
		{
			return y;
		}
		y = y * HEIGHT / (float)Screen.height;
		return y;
	}
}
