using UnityEngine;

// The camera: over-the-shoulder behind your scientist while playing (zooms in when you aim),
// and a slow fly-around of the map behind the main menu.
public class GameCam : MonoBehaviour
{
    public static GameCam I { get; private set; }
    public Camera cam { get; private set; }

    public float yaw, pitch = 10f;          // pitch: + = looking down
    float shake, dist = 3.4f, side = 0.55f;
    Vector3 vel;
    float menuAngle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => I = null;

    public static GameCam Ensure()
    {
        if (I != null) return I;
        Camera c = Camera.main;
        if (c == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            c = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        I = c.GetComponent<GameCam>();
        if (I == null) I = c.gameObject.AddComponent<GameCam>();
        I.cam = c;
        c.nearClipPlane = 0.08f;
        c.farClipPlane = 900f;
        c.clearFlags = CameraClearFlags.Skybox;
        return I;
    }

    public static void Shake(Vector3 from, float amount)
    {
        if (I == null) return;
        float d = Vector3.Distance(from, I.transform.position);
        I.shake = Mathf.Max(I.shake, amount * Mathf.Clamp01(1f - d / 40f));
    }

    public static void Kick(float up) { if (I != null) I.pitch -= up; }

    // Middle of the screen, where your shots go.
    public Ray AimRay => new Ray(transform.position, transform.forward);

    public Vector3 AimPoint(float maxDist = 250f)
    {
        Ray r = AimRay;
        if (Physics.Raycast(r, out RaycastHit hit, maxDist, Combat.NotPlayer, QueryTriggerInteraction.Ignore)) return hit.point;
        return r.origin + r.direction * maxDist;
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        Player p = Player.Local;
        if (p != null && MLGame.Playing)
        {
            bool aim = p.Aiming;
            if (Settings.FirstPerson && p.Alive)
            {
                // 1st person: the camera is your scientist's eyes
                p.Model.SetFirstPerson(true);
                cam.nearClipPlane = 0.05f;
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, aim ? 55f : (p.Sprinting ? 80f : 74f), Mathf.Clamp01(dt * 8f));
                pitch = Mathf.Clamp(pitch, -80f, 80f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                transform.position = p.Model.EyePosition;
                dist = 0.5f;
                if (shake > 0f) { transform.position += Random.insideUnitSphere * shake * 0.1f; shake = Mathf.MoveTowards(shake, 0f, dt * 2.5f); }
                return;
            }
            p.Model.SetFirstPerson(false);
            cam.nearClipPlane = 0.08f;
            float wantDist = aim ? 1.8f : 3.4f, wantSide = aim ? 0.7f : 0.55f;
            if (!p.Alive) { wantDist = 5.5f; wantSide = 0f; }
            dist = Mathf.Lerp(dist, wantDist, Mathf.Clamp01(dt * 10f));
            side = Mathf.Lerp(side, wantSide, Mathf.Clamp01(dt * 10f));
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, aim ? 50f : (p.Sprinting ? 72f : 66f), Mathf.Clamp01(dt * 8f));
            pitch = Mathf.Clamp(pitch, -70f, 75f);
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = p.transform.position + Vector3.up * 1.6f;
            Vector3 shoulder = pivot + rot * new Vector3(side, 0f, 0f);
            // don't let the camera go into walls
            Vector3 back = rot * Vector3.back;
            float d = dist;
            if (Physics.SphereCast(pivot, 0.22f, (shoulder + back * dist - pivot).normalized, out RaycastHit hit, Vector3.Distance(pivot, shoulder + back * dist), Combat.NotPlayer, QueryTriggerInteraction.Ignore))
                d = Mathf.Max(0.3f, hit.distance - 0.1f);
            Vector3 want = d < dist ? pivot + (shoulder + back * dist - pivot).normalized * d : shoulder + back * dist;
            transform.position = want;
            transform.rotation = rot;
        }
        else
        {
            // menu: slow circle above the arena
            menuAngle += dt * 4f;
            Vector3 c = MLGame.ArenaCenter;
            float r = MLGame.ArenaRadius * 0.85f;
            Vector3 pos = c + Quaternion.Euler(0f, menuAngle, 0f) * new Vector3(0f, 0f, -r) + Vector3.up * (26f + Mathf.Sin(menuAngle * 0.05f) * 6f);
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation((c + Vector3.up * 2f) - pos);
            cam.fieldOfView = 55f;
        }
        if (shake > 0f)
        {
            transform.position += Random.insideUnitSphere * shake * 0.25f;
            shake = Mathf.MoveTowards(shake, 0f, dt * 2.5f);
        }
    }
}
