using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

// Explicitly activated local review helper. It never sends chat or changes server/game policy.
[InitializeOnLoad]
public static class WorldChatUiReview
{
    [Serializable] private class Request { public string id, action, path; public int index, width, height; }
    [Serializable] private class Response { public string id, status, detail; }
    private const string Active = "WorldChatUiReview.Active";
    private const string Command = "Logs/world-chat-ui-command.json";
    private const string Result = "Logs/world-chat-ui-result.json";
    private static double nextRead;
    static WorldChatUiReview() { EditorApplication.update += Update; }
    [MenuItem("Tools/World chat/Start UI review")]
    public static void Inspect()
    {
        SessionState.SetBool(Active, true);
        EditorApplication.delayCall += () => Respond("initial", "ok", Dump());
    }
    private static Type Find(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            try { foreach (var type in assembly.GetTypes()) if (type.Name == name) return type; }
            catch (ReflectionTypeLoadException) { }
        return null;
    }
    private static string Dump()
    {
        var text = new StringBuilder();
        foreach (string name in new[] { "SimulatorWindow", "DeviceSimulatorMain", "ScreenSimulation", "UserInterfaceController", "DeviceInfoAsset", "GameView", "GameViewSizes", "GameViewSize", "PlayModeView" })
        {
            Type type = Find(name); text.AppendLine("TYPE " + name + " = " + type);
            if (type == null) continue;
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) text.AppendLine(" FIELD " + field.Name + " " + field.FieldType);
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) text.AppendLine(" PROPERTY " + property.Name + " " + property.PropertyType);
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) text.AppendLine(" METHOD " + method);
        }
        return text.ToString();
    }
    private static void Respond(string id, string status, string detail)
    {
        File.WriteAllText(Result, JsonUtility.ToJson(new Response { id = id, status = status, detail = detail }, true));
    }
    private static void Update()
    {
        if (!SessionState.GetBool(Active, false) || EditorApplication.timeSinceStartup < nextRead || !File.Exists(Command)) return;
        nextRead = EditorApplication.timeSinceStartup + 0.5;
        Request request = null;
        try
        {
            request = JsonUtility.FromJson<Request>(File.ReadAllText(Command));
            if (request == null || request.id == SessionState.GetString("WorldChatUiReview.Last", "")) return;
            SessionState.SetString("WorldChatUiReview.Last", request.id);
            switch (request.action)
            {
                case "inspect": Respond(request.id, "ok", Dump()); break;
                case "state": Respond(request.id, "ok", State()); break;
                case "simulator":
                    Type sim = Find("SimulatorWindow");
                    var window = EditorWindow.GetWindow(sim); window.Show(); window.Focus();
                    Respond(request.id, "ok", Describe(window)); break;
                case "devices":
                    var simulator = EditorWindow.GetWindow(Find("SimulatorWindow"));
                    object main = simulator.GetType().GetProperty("main").GetValue(simulator);
                    var devices = (Array)main.GetType().GetProperty("devices").GetValue(main);
                    var list = new StringBuilder();
                    for (int i = 0; i < devices.Length; i++) list.AppendLine(i + ": " + ((UnityEngine.Object)devices.GetValue(i)).name);
                    Respond(request.id, "ok", list + "\n" + Describe(main)); break;
                case "device":
                    var simWindow = EditorWindow.GetWindow(Find("SimulatorWindow"));
                    object deviceMain = simWindow.GetType().GetProperty("main").GetValue(simWindow);
                    deviceMain.GetType().GetProperty("deviceIndex").SetValue(deviceMain, request.index);
                    object deviceUi = deviceMain.GetType().GetField("m_UserInterface", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(deviceMain);
                    deviceUi.GetType().GetProperty("Rotation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(deviceUi, 90);
                    simWindow.Show(); simWindow.Focus(); simWindow.Repaint();
                    foreach (var other in Resources.FindObjectsOfTypeAll(Find("GameView"))) ((EditorWindow)other).Close();
                    if (request.width > 0) Screen.SetResolution(request.width, request.height, false);
                    Respond(request.id, "ok", "Selected " + request.index + "\n" + Describe(deviceMain)); break;
                case "reflow":
                    ScaleGUI.initScaleGUI(); GameMidlet.gameCanvas.initGameCanvas();
                    GameScr.gamePad = new GamePad(); GameScr.loadCamera(true, Char.myCharz().cx, Char.myCharz().cy);
                    Respond(request.id, "ok", State()); break;
                case "validate":
                    var obstacles = new System.Collections.Generic.List<Rect>(); typeof(GameScr).GetMethod("CollectChatObstacles", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { obstacles });
                    Rect historyBox = WorldChat.WorldChatLayout.HistoryBounds(); int checkedPoints = 0, freePoints = 0;
                    if (historyBox.width == 0 || historyBox.height < 86 || historyBox.x < 0 || historyBox.y < 0 || historyBox.xMax > GameCanvas.w || historyBox.yMax > GameCanvas.h) throw new InvalidOperationException("History outside screen or unavailable: " + historyBox + "; controls=" + string.Join(";", obstacles.Select(o => o.ToString()).ToArray()));
                    foreach (Rect obstacle in obstacles) if (obstacle.width > 0 && obstacle.height > 0 && obstacle.Overlaps(historyBox)) throw new InvalidOperationException("History overlaps " + obstacle);
                    for (int y = GameCanvas.h - 14; y <= GameCanvas.h; y++) for (int x = 0; x < GameCanvas.w; x++) {
                        var point = new Vector2(x + .5f, y + .5f); bool expected = WorldChat.WorldChatLayout.BarBounds.Contains(point);
                        foreach (Rect obstacle in obstacles) if (obstacle.Contains(point)) expected = false;
                        if (WorldChat.WorldChatLayout.CanTapBar(point) != expected) throw new InvalidOperationException("Chat steals control at " + point);
                        checkedPoints++; if (expected) freePoints++;
                    }
                    if (freePoints == 0) throw new InvalidOperationException("No usable chat target");
                    WorldChat.WorldChatHud.Open();
                    if (!(GameCanvas.currentDialog is WorldChat.WorldChatDialog)) throw new InvalidOperationException("History did not open");
                    GameCanvas.px = 0; GameCanvas.py = GameCanvas.h - 1; GameCanvas.isPointerJustRelease = true; GameCanvas.isPointerClick = true;
                    GameCanvas.currentDialog.update();
                    if (GameCanvas.currentDialog != null || GameCanvas.isPointerJustRelease) throw new InvalidOperationException("Outside tap did not close/consume");
                    Respond(request.id, "ok", "PASS; barPoints=" + checkedPoints + "; freePoints=" + freePoints + "; history=" + historyBox + "; outsideTap=closed+consumed; viewport=" + ScaleGUI.Viewport); break;
                case "fixture":
                    var entries = new System.Collections.Generic.List<WorldChat.WorldChatEntry>();
                    for (int i = 1; i <= 30; i++) entries.Add(new WorldChat.WorldChatEntry { Id = i, SenderId = -1, Sender = "QA", Text = "Tin kiểm tra bố cục " + i });
                    WorldChat.WorldChatStore.Snapshot(entries, 30, 1, 10, 10000000, 100, 5000, 30, 0);
                    Respond(request.id, "ok", "Local UI fixture only: 30 entries; no broadcast/debit"); break;
                case "analog":
                    GameScr.isAnalog = request.index; GameScr.gamePad = new GamePad();
                    GameScr.loadCamera(true, Char.myCharz().cx, Char.myCharz().cy);
                    Respond(request.id, "ok", "Analog=" + request.index + "; " + State()); break;
                case "ticker":
                    WorldChat.WorldChatStore.Append(new WorldChat.WorldChatEntry { Id = 31, SenderId = -1, Sender = "QA", Text = new string('W', 100) }, 31, 2);
                    for (int i = 0; i < 300; i++) WorldChat.WorldChatHud.UpdateInput();
                    int tickerOffset = (int)typeof(WorldChat.WorldChatHud).GetField("offset", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                    if (tickerOffset <= 0) throw new InvalidOperationException("Long message did not scroll");
                    Respond(request.id, "ok", "PASS; longMessageOffset=" + tickerOffset + "; local fixture only"); break;
                case "validate-scroll":
                    WorldChat.WorldChatHud.Open();
                    var dialog = GameCanvas.currentDialog as WorldChat.WorldChatDialog;
                    if (dialog == null) throw new InvalidOperationException("Missing history dialog");
                    var fields = BindingFlags.NonPublic | BindingFlags.Instance;
                    Rect historyList = (Rect)typeof(WorldChat.WorldChatDialog).GetField("history", fields).GetValue(dialog);
                    var scrollField = typeof(WorldChat.WorldChatDialog).GetField("scroll", fields);
                    int oldScroll = (int)scrollField.GetValue(dialog);
                    GameCanvas.px = (int)historyList.center.x; GameCanvas.py = (int)historyList.center.y;
                    GameCanvas.isPointerDown = true; dialog.update();
                    GameCanvas.isPointerDown = true; GameCanvas.py += 4; dialog.update();
                    int newScroll = (int)scrollField.GetValue(dialog);
                    if (newScroll >= oldScroll) throw new InvalidOperationException("History did not scroll");
                    Rect closeBox = (Rect)typeof(WorldChat.WorldChatDialog).GetField("close", fields).GetValue(dialog);
                    GameCanvas.px = (int)closeBox.center.x; GameCanvas.py = (int)closeBox.center.y; GameCanvas.isPointerJustRelease = true;
                    dialog.update();
                    if (GameCanvas.currentDialog != null || GameCanvas.isPointerJustRelease) throw new InvalidOperationException("X did not close/consume");
                    Respond(request.id, "ok", "PASS; scroll=" + oldScroll + "->" + newScroll + "; X=closed+consumed; historyHeight=" + historyList.height); break;
                case "clear-fixture": WorldChat.WorldChatHud.BeginSession(); Respond(request.id, "ok", "Cleared local review data"); break;
                case "history": WorldChat.WorldChatHud.Open(); Respond(request.id, "ok", State()); break;
                case "close-history": if (GameCanvas.currentDialog is WorldChat.WorldChatDialog) ((WorldChat.WorldChatDialog)GameCanvas.currentDialog).Close(); Respond(request.id, "ok", State()); break;
                case "quit": Respond(request.id, "ok", "Exiting review editor"); EditorApplication.Exit(0); break;
                case "refresh": Respond(request.id, "ok", "Refreshing scripts"); EditorApplication.delayCall += AssetDatabase.Refresh; break;
                case "resume": EditorApplication.isPaused = false; Time.timeScale = 1; Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate(); Respond(request.id, "ok", State()); break;
                case "play": EditorApplication.isPaused = false; EditorApplication.isPlaying = true; Respond(request.id, "ok", "Entering play mode"); break;
                case "login":
                    if (GameCanvas.currentScreen is ServerScr) GameCanvas.serverScreen.perform(100, null);
                    if (GameCanvas.loginScr == null) GameCanvas.loginScr = new LoginScr();
                    GameCanvas.endDlg(); GameCanvas.loginScr.switchToMe(); GameCanvas.loginScr.doLogin();
                    Respond(request.id, "ok", State()); break;
                case "stop": EditorApplication.isPlaying = false; Respond(request.id, "ok", "Leaving play mode"); break;
                case "capture":
                    string path = Path.GetFullPath(request.path);
                    string docs = Path.GetFullPath("docs") + Path.DirectorySeparatorChar;
                    if (!path.StartsWith(docs, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Capture must stay in docs");
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    var captureWindow = EditorWindow.GetWindow(Find("SimulatorWindow"));
                    RenderTexture render = null;
                    for (Type t = captureWindow.GetType(); t != null; t = t.BaseType)
                        foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                            if (field.FieldType == typeof(RenderTexture) && field.GetValue(captureWindow) != null) render = (RenderTexture)field.GetValue(captureWindow);
                    if (render == null) throw new InvalidOperationException("Simulator render texture unavailable");
                    RenderTexture previous = RenderTexture.active;
                    var screenshot = new Texture2D(render.width, render.height, TextureFormat.RGB24, false);
                    try { RenderTexture.active = render; screenshot.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0); screenshot.Apply();
                        // D3D render target readback is vertically inverted relative to the Game view.
                        if (SystemInfo.graphicsUVStartsAtTop) {
                            Color32[] pixels = screenshot.GetPixels32();
                            for (int row = 0; row < render.height / 2; row++)
                                for (int col = 0; col < render.width; col++) {
                                    int a = row * render.width + col, b = (render.height - row - 1) * render.width + col;
                                    Color32 swap = pixels[a]; pixels[a] = pixels[b]; pixels[b] = swap;
                                }
                            screenshot.SetPixels32(pixels); screenshot.Apply();
                        }
                        File.WriteAllBytes(path, screenshot.EncodeToPNG()); }
                    finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(screenshot); }
                    Respond(request.id, "ok", State()); break;
                case "finish": SessionState.SetBool(Active, false); Respond(request.id, "ok", "Review helper idle"); break;
                default: throw new InvalidOperationException("Unknown review action");
            }
        }
        catch (Exception e) { Respond(request == null ? "error" : request.id, "error", e.ToString()); }
    }
    private static string State()
    {
        return "scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + "; main=" + UnityEngine.Object.FindObjectOfType<Main>() + "; started=" + Main.started + "; paused=" + EditorApplication.isPaused + "; timeScale=" + Time.timeScale + "; playing=" + EditorApplication.isPlaying + "; compiling=" + EditorApplication.isCompiling + "; screen=" + Screen.width + "x" + Screen.height
            + "; safeArea=" + Screen.safeArea + "; logical=" + GameCanvas.w + "x" + GameCanvas.h
            + "; game=" + (GameCanvas.currentScreen == null ? "null" : GameCanvas.currentScreen.GetType().Name)
            + "; zoom=" + mGraphics.zoomLevel + "; touch=" + GameCanvas.isTouch + "; PC=" + Main.isPC;
    }
    private static string Describe(object value)
    {
        var text = new StringBuilder(value.GetType().FullName + "\n");
        foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            object nested = field.GetValue(value); text.AppendLine(field.Name + " = " + (nested == null ? "null" : nested.GetType().FullName));
            if (nested == null || field.Name.IndexOf("Simulat", StringComparison.OrdinalIgnoreCase) < 0) continue;
            foreach (var child in nested.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) text.AppendLine("  " + child.Name + " " + child.FieldType);
            foreach (var method in nested.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)) text.AppendLine("  " + method);
        }
        return text.ToString();
    }
}
