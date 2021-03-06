
//#define DO_ANIMATE
#define DO_LIGHT_SAMPLING
#define DO_THREADED
// 46 spheres (2 emissive) when enabled; 9 spheres (1 emissive) when disabled
#define DO_BIG_SCENE

using System;
using System.Diagnostics;
using static float3;
using static MathUtil;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;

struct Material
{
    public enum Type { Lambert, Metal, Dielectric };
    public Type type;
    public float3 albedo;
    public float3 emissive;
    public float roughness;
    public float ri;
    public Material(Type t, float3 a, float3 e, float r, float i)
    {
        type = t; albedo = a; emissive = e; roughness = r; ri = i;
    }
    public bool HasEmission => emissive.x > 0 || emissive.y > 0 || emissive.z > 0;
};

class Test
{
    const int DO_SAMPLES_PER_PIXEL = 4;
    const float DO_ANIMATE_SMOOTHING = 0.5f;


    Sphere[] s_Spheres = {
        new Sphere(new float3(0,-100.5f,-1), 100),
        new Sphere(new float3(2,0,-1), 0.5f),
        new Sphere(new float3(0,0,-1), 0.5f),
        new Sphere(new float3(-2,0,-1), 0.5f),
        new Sphere(new float3(2,0,1), 0.5f),
        new Sphere(new float3(0,0,1), 0.5f),
        new Sphere(new float3(-2,0,1), 0.5f),
        new Sphere(new float3(0.5f,1,0.5f), 0.5f),
        new Sphere(new float3(-1.5f,1.5f,0f), 0.3f),
        #if DO_BIG_SCENE
        new Sphere(new float3(4,0,-3), 0.5f), new Sphere(new float3(3,0,-3), 0.5f), new Sphere(new float3(2,0,-3), 0.5f), new Sphere(new float3(1,0,-3), 0.5f), new Sphere(new float3(0,0,-3), 0.5f), new Sphere(new float3(-1,0,-3), 0.5f), new Sphere(new float3(-2,0,-3), 0.5f), new Sphere(new float3(-3,0,-3), 0.5f), new Sphere(new float3(-4,0,-3), 0.5f),
        new Sphere(new float3(4,0,-4), 0.5f), new Sphere(new float3(3,0,-4), 0.5f), new Sphere(new float3(2,0,-4), 0.5f), new Sphere(new float3(1,0,-4), 0.5f), new Sphere(new float3(0,0,-4), 0.5f), new Sphere(new float3(-1,0,-4), 0.5f), new Sphere(new float3(-2,0,-4), 0.5f), new Sphere(new float3(-3,0,-4), 0.5f), new Sphere(new float3(-4,0,-4), 0.5f),
        new Sphere(new float3(4,0,-5), 0.5f), new Sphere(new float3(3,0,-5), 0.5f), new Sphere(new float3(2,0,-5), 0.5f), new Sphere(new float3(1,0,-5), 0.5f), new Sphere(new float3(0,0,-5), 0.5f), new Sphere(new float3(-1,0,-5), 0.5f), new Sphere(new float3(-2,0,-5), 0.5f), new Sphere(new float3(-3,0,-5), 0.5f), new Sphere(new float3(-4,0,-5), 0.5f),
        new Sphere(new float3(4,0,-6), 0.5f), new Sphere(new float3(3,0,-6), 0.5f), new Sphere(new float3(2,0,-6), 0.5f), new Sphere(new float3(1,0,-6), 0.5f), new Sphere(new float3(0,0,-6), 0.5f), new Sphere(new float3(-1,0,-6), 0.5f), new Sphere(new float3(-2,0,-6), 0.5f), new Sphere(new float3(-3,0,-6), 0.5f), new Sphere(new float3(-4,0,-6), 0.5f),
        new Sphere(new float3(1.5f,1.5f,-2), 0.3f),
        #endif // #if DO_BIG_SCENE        
    };

    Material[] s_SphereMats = {
        new Material(Material.Type.Lambert,     new float3(0.8f, 0.8f, 0.8f), new float3(0,0,0), 0, 0),
        new Material(Material.Type.Lambert,     new float3(0.8f, 0.4f, 0.4f), new float3(0,0,0), 0, 0),
        new Material(Material.Type.Lambert,     new float3(0.4f, 0.8f, 0.4f), new float3(0,0,0), 0, 0),
        new Material(Material.Type.Metal,       new float3(0.4f, 0.4f, 0.8f), new float3(0,0,0), 0, 0),
        new Material(Material.Type.Metal,       new float3(0.4f, 0.8f, 0.4f), new float3(0,0,0), 0, 0),
        new Material(Material.Type.Metal,       new float3(0.4f, 0.8f, 0.4f), new float3(0,0,0), 0.2f, 0),
        new Material(Material.Type.Metal,       new float3(0.4f, 0.8f, 0.4f), new float3(0,0,0), 0.6f, 0),
        new Material(Material.Type.Dielectric,  new float3(0.4f, 0.4f, 0.4f), new float3(0,0,0), 0, 1.5f),
        new Material(Material.Type.Lambert,     new float3(0.8f, 0.6f, 0.2f), new float3(30,25,15), 0, 0),
        #if DO_BIG_SCENE
        new Material(Material.Type.Lambert, new float3(0.1f, 0.1f, 0.1f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.2f, 0.2f, 0.2f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.3f, 0.3f, 0.3f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.4f, 0.4f, 0.4f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.5f, 0.5f, 0.5f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.6f, 0.6f, 0.6f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.7f, 0.7f, 0.7f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.8f, 0.8f, 0.8f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.9f, 0.9f, 0.9f), new float3(0,0,0), 0, 0),
        new Material(Material.Type.Metal, new float3(0.1f, 0.1f, 0.1f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.2f, 0.2f, 0.2f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.3f, 0.3f, 0.3f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.4f, 0.4f, 0.4f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.5f, 0.5f, 0.5f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.6f, 0.6f, 0.6f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.7f, 0.7f, 0.7f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.8f, 0.8f, 0.8f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.9f, 0.9f, 0.9f), new float3(0,0,0), 0, 0),
        new Material(Material.Type.Metal, new float3(0.8f, 0.1f, 0.1f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.8f, 0.5f, 0.1f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.8f, 0.8f, 0.1f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.4f, 0.8f, 0.1f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.1f, 0.8f, 0.1f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.1f, 0.8f, 0.5f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.1f, 0.8f, 0.8f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.1f, 0.1f, 0.8f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.5f, 0.1f, 0.8f), new float3(0,0,0), 0, 0),
        new Material(Material.Type.Lambert, new float3(0.8f, 0.1f, 0.1f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.8f, 0.5f, 0.1f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.8f, 0.8f, 0.1f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.4f, 0.8f, 0.1f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.1f, 0.8f, 0.1f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.1f, 0.8f, 0.5f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.1f, 0.8f, 0.8f), new float3(0,0,0), 0, 0), new Material(Material.Type.Lambert, new float3(0.1f, 0.1f, 0.8f), new float3(0,0,0), 0, 0), new Material(Material.Type.Metal, new float3(0.5f, 0.1f, 0.8f), new float3(0,0,0), 0, 0),
        new Material(Material.Type.Lambert, new float3(0.1f, 0.2f, 0.5f), new float3(3,10,20), 0, 0),
        #endif
    };

    SpheresSoA s_SpheresSoA;

    public Test()
    {
        s_SpheresSoA = new SpheresSoA(s_Spheres.Length);
    }

    const float kMinT = 0.001f;
    const float kMaxT = 1.0e7f;
    const int kMaxDepth = 10;


    bool HitWorld(ref Ray r, float tMin, float tMax, ref Hit outHit, ref int outID)
    {
        outID = s_SpheresSoA.HitSpheres(ref r, tMin, tMax, ref outHit);
        return outID != -1;
    }

    bool Scatter(ref Material mat, ref Ray r_in, Hit rec, out float3 attenuation, out Ray scattered, out float3 outLightE, ref int inoutRayCount, ref uint state)
    {
        outLightE = new float3(0, 0, 0);
        if (mat.type == Material.Type.Lambert)
        {
            // random point inside unit sphere that is tangent to the hit point
            float3 target = rec.pos + rec.normal + MathUtil.RandomUnitVector(ref state);
            scattered = new Ray(rec.pos, float3.Normalize(target - rec.pos));
            attenuation = mat.albedo;

            // sample lights
#if DO_LIGHT_SAMPLING
            for (int j = 0; j < s_SpheresSoA.emissiveCount; ++j)
            {
                int i = s_SpheresSoA.emissives[j];
                ref Material sphereMat = ref s_SphereMats[i];
                //@TODO if (&mat == &smat)
                //    continue; // skip self
                var s = s_Spheres[i];

                // create a random direction towards sphere
                // coord system for sampling: sw, su, sv
                float3 sw = Normalize(s.center - rec.pos);
                float3 su = Normalize(Cross(MathF.Abs(sw.x) > 0.01f ? new float3(0, 1, 0) : new float3(1, 0, 0), sw));
                float3 sv = Cross(sw, su);
                // sample sphere by solid angle
                float cosAMax = MathF.Sqrt(MathF.Max(0.0f, 1.0f - s.radius * s.radius / (rec.pos - s.center).SqLength));
                float eps1 = RandomFloat01(ref state), eps2 = RandomFloat01(ref state);
                float cosA = 1.0f - eps1 + eps1 * cosAMax;
                float sinA = MathF.Sqrt(1.0f - cosA * cosA);
                float phi = 2 * PI * eps2;
                float3 l = su * MathF.Cos(phi) * sinA + sv * MathF.Sin(phi) * sinA + sw * cosA;

                // shoot shadow ray
                Hit lightHit = default(Hit);
                int hitID = 0;
                ++inoutRayCount;
                var ray = new Ray(rec.pos, l);
                if (HitWorld(ref ray, kMinT, kMaxT, ref lightHit, ref hitID) && hitID == i)
                {
                    float omega = 2 * PI * (1 - cosAMax);

                    float3 rdir = r_in.dir;
                    Debug.Assert(rdir.IsNormalized);
                    float3 nl = Dot(rec.normal, rdir) < 0 ? rec.normal : -rec.normal;
                    outLightE += (mat.albedo * sphereMat.emissive) * (MathF.Max(0.0f, Dot(l, nl)) * omega / PI);
                }
            }
#endif
            return true;
        }
        else if (mat.type == Material.Type.Metal)
        {
            Debug.Assert(r_in.dir.IsNormalized); Debug.Assert(rec.normal.IsNormalized);
            float3 refl = Reflect(r_in.dir, rec.normal);
            // reflected ray, and random inside of sphere based on roughness
            scattered = new Ray(rec.pos, Normalize(refl + mat.roughness * RandomInUnitSphere(ref state)));
            attenuation = mat.albedo;
            return Dot(scattered.dir, rec.normal) > 0;
        }
        else if (mat.type == Material.Type.Dielectric)
        {
            Debug.Assert(r_in.dir.IsNormalized); Debug.Assert(rec.normal.IsNormalized);
            float3 outwardN;
            float3 rdir = r_in.dir;
            float3 refl = Reflect(rdir, rec.normal);
            float nint;
            attenuation = new float3(1, 1, 1);
            float3 refr;
            float reflProb;
            float cosine;
            if (Dot(rdir, rec.normal) > 0)
            {
                outwardN = -rec.normal;
                nint = mat.ri;
                cosine = mat.ri * Dot(rdir, rec.normal);
            }
            else
            {
                outwardN = rec.normal;
                nint = 1.0f / mat.ri;
                cosine = -Dot(rdir, rec.normal);
            }
            if (Refract(rdir, outwardN, nint, out refr))
            {
                reflProb = Schlick(cosine, mat.ri);
            }
            else
            {
                reflProb = 1;
            }
            if (RandomFloat01(ref state) < reflProb)
                scattered = new Ray(rec.pos, Normalize(refl));
            else
                scattered = new Ray(rec.pos, Normalize(refr));
        }
        else
        {
            attenuation = new float3(1, 0, 1);
            scattered = default(Ray);
            return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    float3 Trace(ref Ray r, int depth, ref int inoutRayCount, ref uint state, bool doMaterialE = true)
    {
        Hit rec = default(Hit);
        int id = 0;
        ++inoutRayCount;
        if (HitWorld(ref r, kMinT, kMaxT, ref rec, ref id))
        {
            Ray scattered;
            float3 attenuation;
            float3 lightE;
            ref Material mat = ref s_SphereMats[id];
            var matE = mat.emissive;
            if (depth < kMaxDepth && Scatter(ref mat, ref r, rec, out attenuation, out scattered, out lightE, ref inoutRayCount, ref state))
            {
                #if DO_LIGHT_SAMPLING
                if (!doMaterialE) matE = new float3(0, 0, 0);
                doMaterialE = (mat.type != Material.Type.Lambert);
                #endif
                return matE + lightE + attenuation * Trace(ref scattered, depth + 1, ref inoutRayCount, ref state, doMaterialE);
            }
            else
            {
                return matE;
            }
        }
        else
        {
            // sky
            float3 unitDir = r.dir;
            float t = 0.5f * (unitDir.y + 1.0f);
            return ((1.0f - t) * new float3(1.0f, 1.0f, 1.0f) + t * new float3(0.5f, 0.7f, 1.0f)) * 0.3f;
        }
    }

    int TraceRowJob(int y, int screenWidth, int screenHeight, int frameCount, float[] backbuffer, ref Camera cam)
    {
        int backbufferIdx = y * screenWidth * 4;
        float invWidth = 1.0f / screenWidth;
        float invHeight = 1.0f / screenHeight;
        float lerpFac = (float)frameCount / (float)(frameCount + 1);
#if DO_ANIMATE
        lerpFac *= DO_ANIMATE_SMOOTHING;
#endif
        int rayCount = 0;
        //for (uint32_t y = start; y < end; ++y)
        {
            uint state = (uint)(y * 9781 + frameCount * 6271) | 1;
            for (int x = 0; x < screenWidth; ++x)
            {
                float3 col = new float3(0, 0, 0);
                for (int s = 0; s < DO_SAMPLES_PER_PIXEL; s++)
                {
                    float u = (x + RandomFloat01(ref state)) * invWidth;
                    float v = (y + RandomFloat01(ref state)) * invHeight;
                    Ray r = cam.GetRay(u, v, ref state);
                    col += Trace(ref r, 0, ref rayCount, ref state);
                }
                col *= 1.0f / (float)DO_SAMPLES_PER_PIXEL;

                ref float bb1 = ref backbuffer[backbufferIdx + 0];
                ref float bb2 = ref backbuffer[backbufferIdx + 1];
                ref float bb3 = ref backbuffer[backbufferIdx + 2];

                float3 prev = new float3(bb1, bb2, bb3);
                col = prev * lerpFac + col * (1 - lerpFac);
                bb1 = col.x;
                bb2 = col.y;
                bb3 = col.z;
                backbufferIdx += 4;
            }
        }
        return rayCount;
    }


    public void DrawTest(float time, int frameCount, int screenWidth, int screenHeight, float[] backbuffer, out int outRayCount)
    {
        int rayCount = 0;
#if DO_ANIMATE
        s_Spheres[1].center.y = MathF.Cos(time)+1.0f;
        s_Spheres[8].center.z = MathF.Sin(time)*0.3f;
#endif
        float3 lookfrom = new float3(0, 2, 3);
        float3 lookat = new float3(0, 0, 0);
        float distToFocus = 3;
        float aperture = 0.1f;
        #if DO_BIG_SCENE
        aperture *= 0.2f;
        #endif

        s_SpheresSoA.Update(s_Spheres, s_SphereMats);

        Camera cam = new Camera(lookfrom, lookat, new float3(0, 1, 0), 60, (float)screenWidth / (float)screenHeight, aperture, distToFocus);

#if DO_THREADED
        Parallel.For<int>(0, screenHeight,
                          () => 0,
                          (j, loop, counter) => { return counter + TraceRowJob(j, screenWidth, screenHeight, frameCount, backbuffer, ref cam); },
                          (x) => Interlocked.Add(ref rayCount, x));
#else
        for (int y = 0; y < screenHeight; ++y)
            rayCount += TraceRowJob(y, screenWidth, screenHeight, frameCount, backbuffer, ref cam);
#endif
        outRayCount = rayCount;
    }
}
