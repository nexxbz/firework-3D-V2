using System.Collections.Generic;
using UnityEngine;

// Fireworks Mini (Unity). Todo se crea por codigo: no necesita escena ni assets.
public class FireworksGame : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { new GameObject("FireworksGame").AddComponent<FireworksGame>(); }

    class Item { public GameObject go; public int type, n; public bool on, fired; public float t; public Color col; }
    class Shot { public Vector3 p, v; public int kind; public Color c; }

    static readonly string[] Names = { "Cohete", "Anillo", "Corazon", "Fuente", "Bateria", "Mortero" };
    static readonly Color[] Cols = {
        new Color(.85f,.23f,.23f), new Color(.23f,.54f,.85f), new Color(.88f,.31f,.55f),
        new Color(.88f,.69f,.19f), new Color(.60f,.35f,.88f), new Color(.23f,.63f,.35f) };

    Camera cam; Light sun; ParticleSystem ps; Material addMat; Shader stdSh, unlitSh;
    readonly List<Item> items = new List<Item>();
    readonly List<Shot> shots = new List<Shot>();
    Item held; int cur;
    float yaw = 180f, pitch, py, vy, day, dayTarget, sc = 1f, sens = 0.12f, msgT;
    Vector3 pos = new Vector3(0, 1.7f, 6);
    Vector2 joy; int joyId = -1, lookId = -1; string msg = "";
    Texture2D circle;
    Vector2 cJoy, cAct, cGrab, cFire, cJump, cType, cTime;

    void Start()
    {
        Application.targetFrameRate = 60;
        stdSh = Shader.Find("Standard"); unlitSh = Shader.Find("Sprites/Default");
        cam = Camera.main;
        if (cam == null) { var cg = new GameObject("Cam"); cam = cg.AddComponent<Camera>(); cg.tag = "MainCamera"; }
        cam.clearFlags = CameraClearFlags.SolidColor; cam.farClipPlane = 700; cam.fieldOfView = 70;
        sun = FindObjectOfType<Light>();
        if (sun == null) { var lg = new GameObject("Sun"); sun = lg.AddComponent<Light>(); sun.type = LightType.Directional; }
        sun.transform.rotation = Quaternion.Euler(50, -30, 0);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 40; RenderSettings.fogEndDistance = 380;
        circle = MakeCircle(64);
        addMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive")) { mainTexture = circle };
        BuildWorld(); MakePS();
    }

    // ---------- utilidades ----------
    Texture2D MakeCircle(int n)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
            float a = Mathf.Clamp01(1f - d); t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Sqrt(a)));
        }
        t.Apply(); return t;
    }

    Color RC() { return Color.HSVToRGB(Random.value, 1f, 1f); }

    GameObject Prim(PrimitiveType t, Vector3 p, Vector3 s, Color c, Transform parent = null, bool lit = true)
    {
        var g = GameObject.CreatePrimitive(t);
        Destroy(g.GetComponent<Collider>());
        if (parent != null) g.transform.SetParent(parent, false);
        g.transform.localPosition = p; g.transform.localScale = s;
        g.GetComponent<Renderer>().material = new Material(lit ? stdSh : unlitSh) { color = c };
        return g;
    }

    void BuildWorld()
    {
        Random.InitState(7);
        var asphalt = new Color(.16f, .17f, .2f); var gray = new Color(.44f, .46f, .5f);
        Prim(PrimitiveType.Plane, Vector3.zero, new Vector3(90, 1, 90), new Color(.11f, .23f, .13f));
        Prim(PrimitiveType.Cube, new Vector3(0, .02f, 0), new Vector3(10, .04f, 500), asphalt);
        Prim(PrimitiveType.Cube, new Vector3(0, .03f, -30), new Vector3(500, .04f, 10), asphalt);
        for (int s = -1; s <= 1; s += 2) Prim(PrimitiveType.Cube, new Vector3(s * 6.1f, .09f, 0), new Vector3(2.2f, .18f, 500), gray);
        for (int i = 0; i < 60; i++) Prim(PrimitiveType.Cube, new Vector3(0, .06f, -240 + i * 8), new Vector3(.25f, .05f, 3), new Color(.9f, .88f, .6f), null, false);
        Prim(PrimitiveType.Cube, new Vector3(0, .04f, -6), new Vector3(16, .06f, 14), new Color(.48f, .35f, .22f));
        Prim(PrimitiveType.Cylinder, new Vector3(-70, .05f, -75), new Vector3(56, .02f, 56), new Color(.1f, .35f, .55f));

        for (int i = 0; i < 36; i++)
        {
            float a = Random.value * 6.283f, r = 22 + Random.value * 80;
            float x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r;
            if (Mathf.Abs(x) < 9) continue;
            float w = 6 + Random.value * 7, h = 5 + Random.value * 8, d = 6 + Random.value * 7;
            var rot = Quaternion.Euler(0, 90 * Random.Range(0, 4), 0);
            var hs = Prim(PrimitiveType.Cube, new Vector3(x, h / 2, z), new Vector3(w, h, d), Color.HSVToRGB(Random.Range(.05f, .15f), .25f, .55f));
            hs.transform.rotation = rot;
            var rf = Prim(PrimitiveType.Cube, new Vector3(x, h + 1, z), new Vector3(w * 1.05f, 2, d * 1.05f), new Color(.42f, .16f, .13f));
            rf.transform.rotation = rot;
            for (int k = -1; k <= 1; k += 2)
            {
                var wn = Prim(PrimitiveType.Cube, new Vector3(x, 2.2f, z) + rot * new Vector3(k * w * .25f, 0, d / 2 + .06f), new Vector3(1.2f, 1.2f, .1f), new Color(1f, .82f, .48f), null, false);
                wn.transform.rotation = rot;
            }
        }
        for (int i = 0; i < 60; i++)
        {
            float a = Random.value * 6.283f, r = 14 + Random.value * 120, x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r;
            if (Mathf.Abs(x) < 8) continue;
            var tr = new GameObject("arbol"); tr.transform.position = new Vector3(x, 0, z);
            Prim(PrimitiveType.Cylinder, new Vector3(0, .75f, 0), new Vector3(.4f, .75f, .4f), new Color(.29f, .19f, .12f), tr.transform);
            Prim(PrimitiveType.Sphere, new Vector3(0, 3, 0), new Vector3(3, 3.5f, 3), new Color(.12f, .3f, .16f), tr.transform);
        }
        for (int i = 0; i < 10; i++)
        {
            var lp = new GameObject("farola"); lp.transform.position = new Vector3(i % 2 == 0 ? 6 : -6, 0, -90 + i * 20);
            Prim(PrimitiveType.Cylinder, new Vector3(0, 3, 0), new Vector3(.2f, 3, .2f), new Color(.2f, .2f, .2f), lp.transform);
            Prim(PrimitiveType.Sphere, new Vector3(0, 6.1f, 0), new Vector3(.6f, .6f, .6f), new Color(1f, .95f, .7f), lp.transform, false);
        }
        Color[] cc = { new Color(.75f,.19f,.19f), new Color(.19f,.31f,.75f), new Color(.82f,.75f,.19f), new Color(.88f,.88f,.88f), new Color(.19f,.63f,.38f) };
        for (int i = 0; i < 10; i++)
        {
            var car = new GameObject("auto");
            car.transform.position = new Vector3(i % 2 == 0 ? 3.4f : -3.4f, 0, -200 + i * 40 + Random.value * 10);
            Prim(PrimitiveType.Cube, new Vector3(0, .65f, 0), new Vector3(1.9f, .7f, 4), cc[i % 5], car.transform);
            Prim(PrimitiveType.Cube, new Vector3(0, 1.25f, -.2f), new Vector3(1.6f, .6f, 2), new Color(.6f, .77f, .85f), car.transform);
            float[] wx = { -.9f, .9f, -.9f, .9f }, wz = { 1.3f, 1.3f, -1.3f, -1.3f };
            for (int k = 0; k < 4; k++)
            {
                var wh = Prim(PrimitiveType.Cylinder, new Vector3(wx[k], .35f, wz[k]), new Vector3(.7f, .15f, .7f), Color.black, car.transform);
                wh.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
        }
        for (int i = 0; i < 14; i++)
        {
            float a = i / 14f * 6.283f + .2f, r = 150 + Random.value * 60;
            Prim(PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), new Vector3(70 + Random.value * 50, 45 + Random.value * 40, 70 + Random.value * 50), new Color(.2f, .33f, .22f));
        }
        Prim(PrimitiveType.Sphere, new Vector3(-150, 190, -300), new Vector3(18, 18, 18), new Color(.95f, .96f, 1f), null, false);
    }

    void MakePS()
    {
        var g = new GameObject("Particulas");
        ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = ps.main; m.loop = true; m.playOnAwake = false; m.maxParticles = 15000;
        m.simulationSpace = ParticleSystemSimulationSpace.World; m.gravityModifier = 0.4f;
        m.startLifetime = 2f; m.startSpeed = 0f; m.startSize = 0.45f;
        var e = ps.emission; e.enabled = false;
        var sh = ps.shape; sh.enabled = false;
        var col = ps.colorOverLifetime; col.enabled = true;
        var gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                   new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .5f), new GradientAlphaKey(0, 1) });
        col.color = gr;
        var r = g.GetComponent<ParticleSystemRenderer>();
        r.material = addMat; r.renderMode = ParticleSystemRenderMode.Billboard;
        ps.Play();
    }

    void E(Vector3 p, Vector3 v, Color c, float life, float size = 0.45f)
    {
        var ep = new ParticleSystem.EmitParams();
        ep.position = p; ep.velocity = v; ep.startColor = c; ep.startLifetime = life; ep.startSize = size;
        ps.Emit(ep, 1);
    }

    // ---------- fuegos ----------
    void Burst(Vector3 p, int kind, Color c)
    {
        Vector3 rt = cam.transform.right; rt.y = 0; rt.Normalize();
        if (kind == 1)
        {
            for (int i = 0; i < 80; i++) { float a = i / 80f * 6.283f; E(p, (rt * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * 11f, c, 2f); }
        }
        else if (kind == 2)
        {
            for (int i = 0; i < 90; i++)
            {
                float a = i / 90f * 6.283f;
                float hx = 16 * Mathf.Pow(Mathf.Sin(a), 3);
                float hy = 13 * Mathf.Cos(a) - 5 * Mathf.Cos(2 * a) - 2 * Mathf.Cos(3 * a) - Mathf.Cos(4 * a);
                E(p, (rt * hx + Vector3.up * hy) * .7f, new Color(1, .2f, .47f), 2.2f);
            }
        }
        else
        {
            int n = kind == 5 ? 420 : 150; float mx = kind == 5 ? 20 : 12;
            for (int i = 0; i < n; i++)
            {
                Color cc = (kind == 5 && Random.value < .5f) ? RC() : c;
                E(p, Random.onUnitSphere * Random.Range(0f, mx), cc, Random.Range(1.4f, 2.8f));
            }
        }
    }

    void Launch(Vector3 p, int kind, Color c, bool big)
    {
        shots.Add(new Shot { p = p + Vector3.up * .9f, kind = kind, c = c,
            v = new Vector3(Random.Range(-1f, 1f), (big ? 34f : 26f) + Random.Range(0f, 4f), Random.Range(-1f, 1f)) });
    }

    Item Spawn(Vector3 p, int type)
    {
        var root = new GameObject("fuego"); root.transform.position = p; Color c = Cols[type];
        if (type == 5) { Prim(PrimitiveType.Cylinder, new Vector3(0, .5f, 0), new Vector3(.45f, .5f, .45f), new Color(.16f, .23f, .19f), root.transform); Prim(PrimitiveType.Cube, new Vector3(0, .06f, 0), new Vector3(.7f, .12f, .7f), c, root.transform); }
        else if (type == 4) Prim(PrimitiveType.Cube, new Vector3(0, .35f, 0), new Vector3(.8f, .7f, .8f), c, root.transform);
        else if (type == 3) Prim(PrimitiveType.Cylinder, new Vector3(0, .3f, 0), new Vector3(.5f, .3f, .5f), c, root.transform);
        else { Prim(PrimitiveType.Cylinder, new Vector3(0, .45f, 0), new Vector3(.32f, .45f, .32f), c, root.transform); Prim(PrimitiveType.Sphere, new Vector3(0, .95f, 0), new Vector3(.32f, .4f, .32f), Color.white, root.transform); }
        var it = new Item { go = root, type = type, col = RC() }; items.Add(it); return it;
    }

    void Remove(int i) { if (held == items[i]) held = null; Destroy(items[i].go); items.RemoveAt(i); }

    // ---------- acciones (botones) ----------
    Vector3 AimGround()
    {
        Vector3 o = cam.transform.position, f = cam.transform.forward, g;
        if (f.y < -.02f) { float t = Mathf.Min(-o.y / f.y, 28f); g = o + f * t; } else g = o + f * 10f;
        g.y = 0; return g;
    }

    Item AimItem(float maxD, float tol)
    {
        Vector3 o = cam.transform.position, f = cam.transform.forward; Item best = null; float bd = tol;
        foreach (var it in items)
        {
            if (it == held) continue;
            Vector3 v = it.go.transform.position + Vector3.up * .5f - o; float t = Vector3.Dot(v, f);
            if (t < 0 || t > maxD) continue;
            float e = (v - f * t).magnitude; if (e < bd) { bd = e; best = it; }
        }
        return best;
    }

    void Say(string s) { msg = s; msgT = Time.time + 1.8f; }
    void Act() { if (held != null) { Say("Suelta lo que llevas"); return; } if (items.Count >= 70) { Say("Demasiados fuegos"); return; } Spawn(AimGround(), cur); }
    void Fire() { var it = AimItem(40, 1.6f); if (it != null && !it.on) { it.on = true; it.t = 0; Say("Encendido"); } else Say("Apunta a un fuego"); }
    void Grab()
    {
        if (held != null) { held.go.transform.position = new Vector3(held.go.transform.position.x, 0, held.go.transform.position.z); held = null; Say("Soltado"); return; }
        var it = AimItem(9, 1.4f); if (it != null) { held = it; Say("Agarrado"); } else Say("Nada para agarrar");
    }

    // ---------- entrada tactil ----------
    static bool In(Vector2 p, Vector2 c, float r) { return (p - c).sqrMagnitude <= r * r; }

    void Lay()
    {
        sc = Screen.height / 720f; float W = Screen.width, H = Screen.height;
        cJoy = new Vector2(160 * sc, 150 * sc); cAct = new Vector2(W - 110 * sc, 110 * sc);
        cGrab = new Vector2(W - 240 * sc, 95 * sc); cFire = new Vector2(W - 90 * sc, 240 * sc);
        cJump = new Vector2(W - 90 * sc, 330 * sc); cType = new Vector2(W - 60 * sc, H - 60 * sc); cTime = new Vector2(W * .5f, H - 60 * sc);
    }

    void HandleInput()
    {
        foreach (var t in Input.touches)
        {
            Vector2 p = t.position; bool end = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
            if (t.phase == TouchPhase.Began)
            {
                if (In(p, cAct, 62 * sc)) Act();
                else if (In(p, cGrab, 48 * sc)) Grab();
                else if (In(p, cFire, 42 * sc)) Fire();
                else if (In(p, cJump, 42 * sc)) { if (py <= .01f) vy = 6.5f; }
                else if (In(p, cType, 40 * sc)) { cur = (cur + 1) % Names.Length; Say(Names[cur] + " listo"); }
                else if (In(p, cTime, 40 * sc)) dayTarget = dayTarget < .5f ? 1f : 0f;
                else if (joyId < 0 && p.x < Screen.width * .4f && p.y < Screen.height * .65f) joyId = t.fingerId;
                else if (lookId < 0) lookId = t.fingerId;
            }
            if (t.fingerId == joyId) { if (end) { joyId = -1; joy = Vector2.zero; } else joy = Vector2.ClampMagnitude((p - cJoy) / (80 * sc), 1f); }
            else if (t.fingerId == lookId)
            {
                if (end) lookId = -1;
                else { yaw += t.deltaPosition.x * sens; pitch = Mathf.Clamp(pitch - t.deltaPosition.y * sens, -80f, 80f); }
            }
        }
        if (Application.isEditor && Input.touchCount == 0)
        {
            joy = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (Input.GetMouseButton(1)) { yaw += Input.GetAxis("Mouse X") * 3f; pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 3f, -80f, 80f); }
            if (Input.GetKeyDown(KeyCode.Space) && py <= .01f) vy = 6.5f;
            if (Input.GetKeyDown(KeyCode.E)) Act(); if (Input.GetKeyDown(KeyCode.F)) Fire(); if (Input.GetKeyDown(KeyCode.G)) Grab();
            if (Input.GetKeyDown(KeyCode.Q)) { cur = (cur + 1) % Names.Length; Say(Names[cur] + " listo"); }
            if (Input.GetKeyDown(KeyCode.T)) dayTarget = dayTarget < .5f ? 1f : 0f;
        }
    }

    // ---------- bucle ----------
    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, .033f);
        Lay(); HandleInput();

        Quaternion yq = Quaternion.Euler(0, yaw, 0);
        pos += (yq * Vector3.forward * joy.y + yq * Vector3.right * joy.x) * 7f * dt;
        Vector2 flat = new Vector2(pos.x, pos.z); if (flat.magnitude > 90f) { flat = flat.normalized * 90f; pos.x = flat.x; pos.z = flat.y; }
        py += vy * dt; vy -= 18f * dt; if (py <= 0) { py = 0; if (vy < 0) vy = 0; }
        cam.transform.position = new Vector3(pos.x, 1.7f + py, pos.z);
        cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        if (held != null) held.go.transform.position = new Vector3(pos.x, 0, pos.z) + yq * Vector3.forward * 3f;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            var it = items[i]; if (!it.on) continue; it.t += dt; Vector3 p = it.go.transform.position;
            if (it.type == 3)
            {
                if (it.t > .6f && it.t < 4.5f) for (int k = 0; k < 14; k++) E(p + Vector3.up * .7f, new Vector3(Random.Range(-2f, 2f), Random.Range(9f, 14f), Random.Range(-2f, 2f)), new Color(1, .7f, .25f), 1f, .3f);
                if (it.t > 5f) Remove(i);
            }
            else if (it.type == 4)
            {
                if (it.n < 10 && it.t > .8f + it.n * .35f) { it.n++; Launch(p, 0, RC(), false); }
                if (it.n >= 10 && it.t > 5f) Remove(i);
            }
            else
            {
                if (!it.fired && it.t > .8f) { it.fired = true; Launch(p, it.type, it.col, it.type == 5); }
                else if (!it.fired) E(p + Vector3.up * 1.1f, new Vector3(Random.Range(-.5f, .5f), 2f, Random.Range(-.5f, .5f)), new Color(1, .8f, .35f), .3f, .25f);
                if (it.fired && it.t > 1f) Remove(i);
            }
        }
        for (int i = shots.Count - 1; i >= 0; i--)
        {
            var s = shots[i]; s.p += s.v * dt; s.v.y -= 9f * dt;
            E(s.p, new Vector3(Random.Range(-.5f, .5f), -1f, Random.Range(-.5f, .5f)), new Color(1, .75f, .38f), .5f, .3f);
            if (s.v.y < 2f) { Burst(s.p, s.kind, s.c); shots.RemoveAt(i); }
        }

        day = Mathf.MoveTowards(day, dayTarget, dt * .5f);
        var bg = Color.Lerp(new Color(.02f, .03f, .1f), new Color(.5f, .72f, .93f), day);
        cam.backgroundColor = bg; RenderSettings.fogColor = Color.Lerp(new Color(.035f, .05f, .11f), new Color(.72f, .83f, .93f), day);
        RenderSettings.ambientLight = Color.Lerp(new Color(.2f, .22f, .35f), new Color(.8f, .8f, .85f), day);
        sun.intensity = Mathf.Lerp(.35f, 1.1f, day);
    }

    // ---------- interfaz ----------
    void Disc(Vector2 c, float r, float a) { GUI.color = new Color(1, 1, 1, a); GUI.DrawTexture(new Rect(c.x - r, Screen.height - c.y - r, r * 2, r * 2), circle); GUI.color = Color.white; }
    void Txt(Vector2 c, string s, int size)
    {
        var st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = size, fontStyle = FontStyle.Bold };
        st.normal.textColor = Color.white; GUI.Label(new Rect(c.x - 120, Screen.height - c.y - 30, 240, 60), s, st);
    }

    void OnGUI()
    {
        if (circle == null) return;
        Disc(cJoy, 100 * sc, .25f); Disc(cJoy + joy * 80 * sc, 40 * sc, .6f);
        Disc(cAct, 62 * sc, .5f); Txt(cAct, "PONER", (int)(20 * sc));
        Disc(cGrab, 48 * sc, .45f); Txt(cGrab, "MANO", (int)(18 * sc));
        Disc(cFire, 42 * sc, .45f); Txt(cFire, "FUEGO", (int)(16 * sc));
        Disc(cJump, 42 * sc, .45f); Txt(cJump, "SALTO", (int)(16 * sc));
        Disc(cType, 40 * sc, .45f); Txt(cType, "TIPO", (int)(16 * sc));
        Disc(cTime, 40 * sc, .45f); Txt(cTime, "HORA", (int)(16 * sc));
        Txt(new Vector2(Screen.width * .5f, Screen.height - 110 * sc), Names[cur], (int)(22 * sc));
        if (Time.time < msgT) Txt(new Vector2(Screen.width * .5f, Screen.height - 150 * sc), msg, (int)(18 * sc));
        Txt(new Vector2(80 * sc, Screen.height - 30 * sc), Mathf.RoundToInt(1f / Mathf.Max(Time.smoothDeltaTime, .0001f)) + " FPS", (int)(18 * sc));
    }
}
