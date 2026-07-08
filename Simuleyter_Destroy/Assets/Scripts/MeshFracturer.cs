using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public static class MeshFracturer
{
    public static List<Mesh> FractureMesh(Mesh original, int chunkCount, int seed)
    {
        if (original == null || original.vertexCount == 0)
        {
            Debug.LogError("[MeshFracturer] Source mesh is empty");
            return new List<Mesh>();
        }

        List<Mesh> chunks = new List<Mesh> { CloneMesh(original) };
        Random rng = new Random(seed);
        int maxAttempts = chunkCount * 30;
        int attempts = 0;

        while (chunks.Count < chunkCount && attempts < maxAttempts)
        {
            attempts++;

            int idx = PickLargeEnoughChunk(chunks, rng);
            if (idx < 0) break;

            Mesh piece = chunks[idx];
            Bounds b = piece.bounds;

            Vector3 normal = RandomDirection(rng);
            Vector3 center = b.center;
            float offset = (float)(rng.NextDouble() - 0.5) * b.extents.magnitude * 0.3f;
            Plane plane = new Plane(normal, center + normal * offset);

            if (SplitMeshByPlane(piece, plane, out Mesh left, out Mesh right))
            {
                if (left.vertexCount >= 3 && right.vertexCount >= 3)
                {
                    chunks.RemoveAt(idx);
                    chunks.Add(left);
                    chunks.Add(right);
                }
            }
        }

        chunks.RemoveAll(m => m.vertexCount < 3);
        return chunks;
    }

    public static bool SplitMeshByPlane(Mesh mesh, Plane plane, out Mesh positive, out Mesh negative)
    {
        positive = null;
        negative = null;

        Vector3[] verts = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uv = mesh.uv;
        int[] tris = mesh.triangles;
        bool hasNormals = normals != null && normals.Length == verts.Length;
        bool hasUV = uv != null && uv.Length == verts.Length;

        float[] distances = new float[verts.Length];

        Dictionary<int, int> posVertMap = new Dictionary<int, int>();
        Dictionary<int, int> negVertMap = new Dictionary<int, int>();

        List<Vector3> posVerts = new List<Vector3>();
        List<Vector3> posNormals = new List<Vector3>();
        List<Vector2> posUV = new List<Vector2>();
        List<int> posTris = new List<int>();

        List<Vector3> negVerts = new List<Vector3>();
        List<Vector3> negNormals = new List<Vector3>();
        List<Vector2> negUV = new List<Vector2>();
        List<int> negTris = new List<int>();

        List<Vector3> capPointsPos = new List<Vector3>();
        List<Vector3> capPointsNeg = new List<Vector3>();

        float eps = 0.0001f;

        for (int i = 0; i < verts.Length; i++)
            distances[i] = plane.GetDistanceToPoint(verts[i]);

        for (int i = 0; i < tris.Length; i += 3)
        {
            int i0 = tris[i];
            int i1 = tris[i + 1];
            int i2 = tris[i + 2];

            float d0 = distances[i0];
            float d1 = distances[i1];
            float d2 = distances[i2];

            int side0 = d0 > eps ? 1 : (d0 < -eps ? -1 : 0);
            int side1 = d1 > eps ? 1 : (d1 < -eps ? -1 : 0);
            int side2 = d2 > eps ? 1 : (d2 < -eps ? -1 : 0);
            int sum = side0 + side1 + side2;

            if (sum == 3 || (sum == 2 && side0 >= 0 && side1 >= 0 && side2 >= 0))
            {
                AddTriangle(posVerts, posNormals, posUV, posTris, posVertMap,
                    verts, normals, uv, hasNormals, hasUV, i0, i1, i2);
                continue;
            }

            if (sum == -3 || (sum == -2 && side0 <= 0 && side1 <= 0 && side2 <= 0))
            {
                AddTriangle(negVerts, negNormals, negUV, negTris, negVertMap,
                    verts, normals, uv, hasNormals, hasUV, i0, i1, i2);
                continue;
            }

            SplitTriangle(
                verts, normals, uv, hasNormals, hasUV,
                i0, i1, i2, d0, d1, d2,
                posVerts, posNormals, posUV, posTris, posVertMap,
                negVerts, negNormals, negUV, negTris, negVertMap,
                capPointsPos, capPointsNeg);
        }

        if (capPointsPos.Count >= 3)
            AddCap(capPointsPos, plane.normal, posVerts, posNormals, posUV, posTris);

        if (capPointsNeg.Count >= 3)
            AddCap(capPointsNeg, -plane.normal, negVerts, negNormals, negUV, negTris);

        if (posVerts.Count >= 3)
        {
            positive = new Mesh();
            positive.SetVertices(posVerts);
            if (hasNormals) positive.SetNormals(posNormals);
            else positive.RecalculateNormals();
            if (hasUV) positive.SetUVs(0, posUV);
            positive.SetTriangles(posTris, 0);
            positive.RecalculateBounds();
            positive.RecalculateTangents();
        }

        if (negVerts.Count >= 3)
        {
            negative = new Mesh();
            negative.SetVertices(negVerts);
            if (hasNormals) negative.SetNormals(negNormals);
            else negative.RecalculateNormals();
            if (hasUV) negative.SetUVs(0, negUV);
            negative.SetTriangles(negTris, 0);
            negative.RecalculateBounds();
            negative.RecalculateTangents();
        }

        return positive != null && negative != null;
    }

    private static int PickLargeEnoughChunk(List<Mesh> chunks, Random rng)
    {
        List<int> candidates = new List<int>();
        int maxVerts = 0;
        for (int i = 0; i < chunks.Count; i++)
            if (chunks[i].vertexCount > maxVerts)
                maxVerts = chunks[i].vertexCount;

        int threshold = Mathf.Max(10, maxVerts / 2);

        for (int i = 0; i < chunks.Count; i++)
            if (chunks[i].vertexCount >= threshold)
                candidates.Add(i);

        if (candidates.Count == 0) return -1;
        return candidates[rng.Next(candidates.Count)];
    }

    private static Vector3 RandomDirection(Random rng)
    {
        double theta = rng.NextDouble() * Mathf.PI * 2;
        double phi = System.Math.Acos(2 * rng.NextDouble() - 1);
        return new Vector3(
            (float)(System.Math.Sin(phi) * System.Math.Cos(theta)),
            (float)(System.Math.Sin(phi) * System.Math.Sin(theta)),
            (float)System.Math.Cos(phi)
        ).normalized;
    }

    private static Mesh CloneMesh(Mesh src)
    {
        Mesh clone = new Mesh();
        clone.vertices = src.vertices;
        clone.normals = src.normals;
        clone.uv = src.uv;
        clone.triangles = src.triangles;
        clone.subMeshCount = src.subMeshCount;
        clone.bounds = src.bounds;
        return clone;
    }

    private static void AddTriangle(
        List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris,
        Dictionary<int, int> vertMap,
        Vector3[] srcVerts, Vector3[] srcNormals, Vector2[] srcUV, bool hasNorm, bool hasUV,
        int i0, int i1, int i2)
    {
        tris.Add(GetOrAddVertex(verts, normals, uvs, vertMap, srcVerts, srcNormals, srcUV, hasNorm, hasUV, i0));
        tris.Add(GetOrAddVertex(verts, normals, uvs, vertMap, srcVerts, srcNormals, srcUV, hasNorm, hasUV, i1));
        tris.Add(GetOrAddVertex(verts, normals, uvs, vertMap, srcVerts, srcNormals, srcUV, hasNorm, hasUV, i2));
    }

    private static int GetOrAddVertex(
        List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs,
        Dictionary<int, int> vertMap,
        Vector3[] srcVerts, Vector3[] srcNormals, Vector2[] srcUV, bool hasNorm, bool hasUV,
        int srcIndex)
    {
        if (vertMap.TryGetValue(srcIndex, out int existing))
            return existing;

        int newIndex = verts.Count;
        verts.Add(srcVerts[srcIndex]);
        if (hasNorm) normals.Add(srcNormals[srcIndex]);
        if (hasUV) uvs.Add(srcUV[srcIndex]);
        vertMap[srcIndex] = newIndex;
        return newIndex;
    }

    private static void SplitTriangle(
        Vector3[] srcVerts, Vector3[] srcNormals, Vector2[] srcUV, bool hasNorm, bool hasUV,
        int i0, int i1, int i2, float d0, float d1, float d2,
        List<Vector3> posVerts, List<Vector3> posNormals, List<Vector2> posUV, List<int> posTris,
        Dictionary<int, int> posVertMap,
        List<Vector3> negVerts, List<Vector3> negNormals, List<Vector2> negUV, List<int> negTris,
        Dictionary<int, int> negVertMap,
        List<Vector3> capPos, List<Vector3> capNeg)
    {
        float eps = 0.0001f;

        int[] sides = new int[3];
        sides[0] = d0 > eps ? 1 : (d0 < -eps ? -1 : 0);
        sides[1] = d1 > eps ? 1 : (d1 < -eps ? -1 : 0);
        sides[2] = d2 > eps ? 1 : (d2 < -eps ? -1 : 0);

        int[] idx = { i0, i1, i2 };
        float[] dist = { d0, d1, d2 };

        int loneIdx = -1;
        int loneSign = 0;

        for (int i = 0; i < 3; i++)
        {
            int cnt = 0;
            for (int j = 0; j < 3; j++)
                if (j != i && System.Math.Sign(sides[j]) == System.Math.Sign(sides[i]))
                    cnt++;

            if (cnt == 0)
            {
                loneIdx = i;
                loneSign = sides[i];
                break;
            }
        }

        if (loneIdx < 0) return;

        int other1 = (loneIdx + 1) % 3;
        int other2 = (loneIdx + 2) % 3;

        Vector3 pLone = srcVerts[idx[loneIdx]];
        Vector3 p1 = srcVerts[idx[other1]];
        Vector3 p2 = srcVerts[idx[other2]];

        float t1 = dist[loneIdx] / (dist[loneIdx] - dist[other1]);
        float t2 = dist[loneIdx] / (dist[loneIdx] - dist[other2]);

        Vector3 intersection1 = Vector3.Lerp(pLone, p1, t1);
        Vector3 intersection2 = Vector3.Lerp(pLone, p2, t2);

        Vector3 nLone = hasNorm ? srcNormals[idx[loneIdx]] : Vector3.zero;
        Vector3 n1 = hasNorm ? srcNormals[idx[other1]] : Vector3.zero;
        Vector3 n2 = hasNorm ? srcNormals[idx[other2]] : Vector3.zero;
        Vector3 nInt1 = hasNorm ? Vector3.Lerp(nLone, n1, t1).normalized : Vector3.zero;
        Vector3 nInt2 = hasNorm ? Vector3.Lerp(nLone, n2, t2).normalized : Vector3.zero;

        Vector2 uvLone = hasUV ? srcUV[idx[loneIdx]] : Vector2.zero;
        Vector2 uv1 = hasUV ? srcUV[idx[other1]] : Vector2.zero;
        Vector2 uv2 = hasUV ? srcUV[idx[other2]] : Vector2.zero;
        Vector2 uvInt1 = hasUV ? Vector2.Lerp(uvLone, uv1, t1) : Vector2.zero;
        Vector2 uvInt2 = hasUV ? Vector2.Lerp(uvLone, uv2, t2) : Vector2.zero;

        List<Vector3> loneVerts = loneSign > 0 ? posVerts : negVerts;
        List<Vector3> loneNormals = loneSign > 0 ? posNormals : negNormals;
        List<Vector2> loneUV = loneSign > 0 ? posUV : negUV;
        List<int> loneTris = loneSign > 0 ? posTris : negTris;
        Dictionary<int, int> loneVertMap = loneSign > 0 ? posVertMap : negVertMap;

        List<Vector3> pairVerts = loneSign > 0 ? negVerts : posVerts;
        List<Vector3> pairNormals = loneSign > 0 ? negNormals : posNormals;
        List<Vector2> pairUV = loneSign > 0 ? negUV : posUV;
        List<int> pairTris = loneSign > 0 ? negTris : posTris;
        Dictionary<int, int> pairVertMap = loneSign > 0 ? negVertMap : posVertMap;

        List<Vector3> cap = loneSign > 0 ? capPos : capNeg;

        AddUniqueCapPoint(cap, intersection1);
        AddUniqueCapPoint(cap, intersection2);

        int vLone = GetOrAddVertex(loneVerts, loneNormals, loneUV, loneVertMap,
            srcVerts, srcNormals, srcUV, hasNorm, hasUV, idx[loneIdx]);
        int vInt1Lone = AddTempVertex(loneVerts, loneNormals, loneUV, intersection1, nInt1, uvInt1);
        int vInt2Lone = AddTempVertex(loneVerts, loneNormals, loneUV, intersection2, nInt2, uvInt2);

        loneTris.Add(vLone);
        loneTris.Add(vInt1Lone);
        loneTris.Add(vInt2Lone);

        int v1 = GetOrAddVertex(pairVerts, pairNormals, pairUV, pairVertMap,
            srcVerts, srcNormals, srcUV, hasNorm, hasUV, idx[other1]);
        int v2 = GetOrAddVertex(pairVerts, pairNormals, pairUV, pairVertMap,
            srcVerts, srcNormals, srcUV, hasNorm, hasUV, idx[other2]);
        int vInt1Pair = AddTempVertex(pairVerts, pairNormals, pairUV, intersection1, nInt1, uvInt1);
        int vInt2Pair = AddTempVertex(pairVerts, pairNormals, pairUV, intersection2, nInt2, uvInt2);

        pairTris.Add(v1);
        pairTris.Add(v2);
        pairTris.Add(vInt1Pair);

        pairTris.Add(v2);
        pairTris.Add(vInt2Pair);
        pairTris.Add(vInt1Pair);
    }

    private static int AddTempVertex(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs,
        Vector3 pos, Vector3 normal, Vector2 uv)
    {
        verts.Add(pos);
        normals.Add(normal);
        uvs.Add(uv);
        return verts.Count - 1;
    }

    private static void AddUniqueCapPoint(List<Vector3> capPoints, Vector3 point)
    {
        float threshold = 0.001f;
        for (int i = 0; i < capPoints.Count; i++)
        {
            if (Vector3.Distance(capPoints[i], point) < threshold)
                return;
        }
        capPoints.Add(point);
    }

    private static void AddCap(List<Vector3> capPoints, Vector3 normal,
        List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris)
    {
        if (capPoints.Count < 3) return;

        Vector3 center = Vector3.zero;
        for (int i = 0; i < capPoints.Count; i++)
            center += capPoints[i];
        center /= capPoints.Count;

        Vector3 refVec = (capPoints[0] - center).normalized;
        Vector3 up = normal;
        Vector3 right = Vector3.Cross(up, refVec).normalized;
        Vector3 fwd = Vector3.Cross(right, up).normalized;

        capPoints.Sort((a, b) =>
        {
            Vector3 da = a - center;
            Vector3 db = b - center;
            float angleA = Mathf.Atan2(Vector3.Dot(da, right), Vector3.Dot(da, fwd));
            float angleB = Mathf.Atan2(Vector3.Dot(db, right), Vector3.Dot(db, fwd));
            return angleA.CompareTo(angleB);
        });

        int centerIdx = verts.Count;
        verts.Add(center);
        normals.Add(normal);
        uvs.Add(Vector2.zero);

        for (int i = 0; i < capPoints.Count; i++)
        {
            int i0 = AddTempVertex(verts, normals, uvs, capPoints[i], normal, Vector2.zero);
            int i1 = AddTempVertex(verts, normals, uvs, capPoints[(i + 1) % capPoints.Count], normal, Vector2.zero);
            tris.Add(i0);
            tris.Add(centerIdx);
            tris.Add(i1);
        }
    }
}
